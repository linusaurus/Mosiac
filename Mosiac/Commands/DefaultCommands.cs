using System;
using System.Text;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PurchaseModel;
using System.Reflection;
using System.Data.SqlClient;
using System.Collections.Generic;

namespace Mosiac.Commands
{
    // Must be a public static class:
    public static class DefaultCommands
    {
        // Methods used as console commands must be public and must return a string

        public static string showpart(int id,string flag = "-")
        {
            int rightspace = 25;
            StringBuilder sb = new StringBuilder();
         

                using (var ctx = new MyContext())
                {
                    try
                    {
                        Part p = ctx.Part.Where(c => c.PartID == id).Single();
                        sb.AppendLine("--->"); //Spacer-Header
                        sb.AppendLine("");
                        sb.Append("PartID".PadRight(rightspace));
                        sb.AppendLine(p.PartID.ToString());
                        //--
                        sb.Append("ItemName".PadRight(rightspace));
                        if (p.ItemName != null) sb.AppendLine(p.ItemName); else { sb.AppendLine(""); }
                        //--
                        sb.Append("PartNum".PadRight(rightspace));
                        if (p.PartNum != null) sb.AppendLine(p.PartNum); else { sb.Append(""); }
                        //--
                        sb.Append("Item Description".PadRight(rightspace));
                        sb.AppendLine(p.ItemDescription);

                        sb.Append("ManuID".PadRight(rightspace));
                        sb.AppendLine(p.ManuID.ToString());

                        sb.Append("Cost".PadRight(rightspace));
                        sb.AppendLine(p.Cost.Value.ToString("C2"));

                        sb.Append("Supplier".PadRight(rightspace));
                        if (p.SupplierID.HasValue)
                        {
                            try
                            {
                                var supplier = ctx.Supplier.Where(s => s.SupplierID == p.SupplierID.Value).Single();
                                sb.AppendLine(supplier.SupplierName);
                            }
                            catch
                            {

                                sb.AppendLine("");
                            }

                        }
                        else { sb.AppendLine("--"); }



                        sb.Append("Supplier Description".PadRight(rightspace));
                        sb.AppendLine(p.SupplierDescription);

                        sb.Append("SKU".PadRight(rightspace));
                        sb.AppendLine(p.SKU);

                        sb.Append("Added By".PadRight(rightspace));
                        sb.AppendLine(p.AddedBy.ToString().TrimEnd());


                        sb.Append("Unit OF Measure".PadRight(rightspace));
                        if (p.UID.HasValue)
                        {
                            var uid = ctx.UnitOfMeasure.Where(w => w.UID == p.UID.Value).Single();
                            sb.AppendLine(uid.UOM);
                        }
                        decimal inventoryCount = ctx.Inventory.Where(l => l.PartID == id).Sum(c => c.Qnty);
                        sb.Append("Current Stock".PadRight(rightspace));
                        sb.AppendLine(inventoryCount.ToString());
                        sb.AppendLine("");
                        sb.Append("<---");

                    }
                    catch { sb.AppendLine("No Valid Part Found"); }

                }
            

            return sb.ToString();
            
            
        }

        public static string showorder(int orderNum)
        {
            StringBuilder sb = new StringBuilder();
            using (var ctx = new MyContext())
            {
                sb.AppendLine(Filler(120));
                sb.AppendLine(String.Format("|{0,-8}|{1,-10}|{2,-12}|{3,-30}|{4,-18}|{5,-37}|",
                       "OrderID", "Date","Total","Supplier", "Employee", "Job"));
                sb.AppendLine(Filler(120));
                try
                {

                    var po = ctx.PurchaseOrder.Include(s => s.Supplier).Include(l => l.PurchaseLineItems)
                        .Include(e=> e.Employee).Include(j=> j.Job)
                                                .Where(j => j.OrderNum == orderNum).First();

                   sb.AppendLine(String.Format("|{0,-8}|{1,-10}|{2,-12}|{3,-30}|{4,-18}|{5,-37}|",
                        po.OrderNum,po.OrderDate.Value.ToShortDateString(),po.OrderTotal.Value.ToString("C2"),po.Supplier.SupplierName,po.Employee.firstname +" " + po.Employee.lastname,StringTool.Truncate(po.Job.jobName.ToString().TrimEnd(),37)));
                    sb.AppendLine(Filler(120));//-------------------
                    sb.AppendLine(FillDown(2));
                    sb.AppendLine(Filler(120));//-------------------
                    sb.AppendLine(String.Format("|{0,-8}|{1,-6}|{2,-8}|{3,-68}|{4,8}|{5,12}", "LineID", "PartID","Qnty", "Description", "Cost","Extend"));
                    sb.AppendLine(Filler(120));//---------------
                    string desc;
                    foreach (var pline in po.PurchaseLineItems)
                    {
                        if(pline.Description.TrimEnd().Length > 66)
                        {desc = StringTool.Truncate(pline.Description.ToString().TrimEnd(), 62) + "...";}
                        else
                        {desc = pline.Description.ToString().TrimEnd(); }
                                
                        string cost=  pline.UnitCost.Value.ToString();
                        sb.AppendLine(String.Format("|{0,-8}|{1,-6}|{2,-8}|{3,-68}|{4,8}|{5,17}|",
                            pline.LineID,pline.PartID,String.Format("{0:0.00}",pline.Qnty),desc,pline.UoPPrice.Value.ToString("C2"),pline.Extended.Value.ToString("C2")));
                    }

                    sb.AppendLine(FillDown(2));
                    sb.AppendLine(String.Format("Order Total = {0,8}",po.OrderTotal.Value.ToString("C2")));
                    string revd = string.Empty;
                    if (po.Recieved.Value)
                    { revd = "YES"; }
                    else{ revd = "NO"; }

                    sb.Append(String.Format("Received-{0}  ",revd));
                    if (po.RecievedDate.HasValue)
                    {
                        sb.AppendLine(String.Format("Received Date = {0} ", po.RecievedDate.Value.ToShortDateString()));
                    }
                    else
                    {
                        sb.AppendLine(String.Format("Received Date = {0} ", "NA"));
                    }

                }
                catch (Exception ex)
                {
                    sb.AppendLine("Order does not exist or has been deleted");

                }


            }
            return sb.ToString() ;
        }

        public static string showjoborders(int jobNum, String supplier = "s")
        {
            StringBuilder sb = new StringBuilder();
            decimal total = Decimal.Zero;
            using (var ctx = new MyContext())
            {
                sb.AppendLine(Filler(120));
                sb.AppendLine(String.Format("|{0,-8}|{1,-10}|{2,-12}|{3,-30}|{4,-18}|{5,-37}|",
                       "OrderID", "Date", "Total", "Supplier", "Employee", "Job"));
                sb.AppendLine(Filler(120));
                decimal jobTotal = 0.0m;
                
                try
                {
                    // If no supplier is given
                    if (supplier == "s")
                    {
                       var pos = ctx.PurchaseOrder.Include(s => s.Supplier)
                            .Include(e => e.Employee).Include(j => j.Job)
                                                    .Where(j => j.Job_id == jobNum).ToList();
                   

                    foreach(PurchaseOrder po in pos)
                    {
                        sb.AppendLine(String.Format("|{0,-8}|{1,-10}|{2,-12}|{3,-30}|{4,-18}|{5,-37}|",
                        po.OrderNum, po.OrderDate.Value.ToShortDateString(), po.OrderTotal.Value.ToString("C2"), po.Supplier.SupplierName, po.Employee.firstname + " " + po.Employee.lastname, StringTool.Truncate(po.Job.jobName.ToString().TrimEnd(), 37)));
                        jobTotal += po.OrderTotal.Value;
                            total += po.OrderTotal.Value;
                    }
                    }

                    // If no supplier is given
                    if (supplier != "s")
                    {
                        var pos = ctx.PurchaseOrder.Include(s => s.Supplier)
                             .Include(e => e.Employee).Include(j => j.Job)
                                                     .Where(j => j.Job_id == jobNum).Where(k=> k.Supplier.SupplierName.Contains(supplier)).ToList();

                        foreach (PurchaseOrder po in pos)
                        {
                            sb.AppendLine(String.Format("|{0,-8}|{1,-10}|{2,-12}|{3,-30}|{4,-18}|{5,-37}|",
                            po.OrderNum, po.OrderDate.Value.ToShortDateString(), po.OrderTotal.Value.ToString("C2"), po.Supplier.SupplierName, po.Employee.firstname + " " + po.Employee.lastname, StringTool.Truncate(po.Job.jobName.ToString().TrimEnd(), 37)));
                            jobTotal += po.OrderTotal.Value;
                            total += po.OrderTotal.Value;
                        }
                    }

                    sb.AppendLine("");
                    sb.AppendLine(Filler(120));
                    sb.AppendLine(String.Format("Job Total = {0}",jobTotal.ToString("C")));
                    sb.AppendLine(Filler(120));


                }
                catch (Exception ex)
                {
                    sb.AppendLine(ex.InnerException.ToString());

                }


            }
            return sb.ToString();
        }

        public static string showsupplierorders(String supplier)
        {
            decimal total = decimal.Zero;
            StringBuilder sb = new StringBuilder();
            using (var ctx = new MyContext())
            {
                sb.AppendLine(Filler(120));
                sb.AppendLine(String.Format("|{0,-8}|{1,-10}|{2,-12}|{3,-30}|{4,-18}|{5,-37}|",
                       "OrderID", "Date", "Total", "Supplier", "Employee", "Job"));
                sb.AppendLine(Filler(120));
                
                try
                {
                   
                        var pos = ctx.PurchaseOrder.Include(s => s.Supplier)
                             .Include(e => e.Employee).Include(j => j.Job).Where(k => k.Supplier.SupplierName.Contains(supplier)).ToList();

                        foreach (PurchaseOrder po in pos)
                        {
                        string  jname = po.Job_id.HasValue ? po.Job.jobName : "Unknown";
                            sb.AppendLine(String.Format("|{0,-8}|{1,-10}|{2,-12}|{3,-30}|{4,-18}|{5,-37}|",
                            po.OrderNum, po.OrderDate.Value.ToShortDateString(), po.OrderTotal.Value.ToString("C2"), 
                            po.Supplier.SupplierName, po.Employee.firstname + " " + po.Employee.lastname, 
                            StringTool.Truncate(jname, 37)));
                             total += po.OrderTotal.Value;
                        }               

                }
                catch (Exception ex)
                {
                    sb.AppendLine(ex.InnerException.ToString());

                }


            }
            sb.AppendLine("");
            sb.AppendLine(Filler(120));
            sb.AppendLine(String.Format("Total Purchases = {0}",total.ToString("C")));
            sb.AppendLine(Filler(120));
            return sb.ToString();
        }

        public static string findline(string search)
        {
            StringBuilder sb = new StringBuilder();
            using (var ctx = new MyContext())
            {
                sb.AppendLine(Filler(120));
                sb.AppendLine(String.Format("|{0,-10}|{1,-10}|{2,-12}|{3,-10}|{4,-74}|","LineID", "Order#", "Date","Qnty", "Discription"));
                sb.AppendLine(Filler(120));
                try
                {
                    var lines = ctx.PurchaseLineItem.Include(v=> v.PurchaseOrder).Where(c => c.Description.Contains(search));
                    foreach (PurchaseLineItem l in lines)
                    {
                       
                        string desc;
                        if (l.Description.ToString().TrimEnd().Length > 70)
                        { desc = StringTool.Truncate(l.Description.ToString().TrimEnd(), 70) + "..."; }
                        else
                        { desc = l.Description.ToString().TrimEnd(); }

                        string qnty = String.Format("{0:0.00}", l.Qnty);
                        string date = l.PurchaseOrder.OrderDate.Value.ToShortDateString();
                        sb.AppendLine(String.Format("|{0,-10}|{1,-10}|{2,-12}|{3,-10}|{4,-74}|",l.LineID,l.PurchaseOrderID,date,qnty,desc));
                    }
             

                }
                catch //(Exception ex)
                {
                    //sb.AppendLine(ex.InnerException.ToString());

                }


            }
            return sb.ToString();
        }

        public static string showtrans(int partID)
        {

            StringBuilder sb = new StringBuilder();
            using (var ctx = new MyContext())
            {
                try
                {

                    var partTransactions = ctx.Inventory.Where(l => l.PartID == partID).ToList();
                    if (partTransactions.Count > 0)
                    {
                        sb.AppendLine(Filler(120));// --
                        sb.AppendLine(String.Format("|{0,-10}|{1,-8}|{2,-12}|{3,-66}|{4,-10}|{5,9}|", "Stock_ID", "Part_ID", "Date","Description","TransType","Qnty")); //header--
                        sb.AppendLine(Filler(120));
                        
                        foreach (Inventory i in partTransactions)
                        {

                            string desc = StringTool.Truncate(i.Description.ToString().TrimEnd(), 65);
                            sb.AppendLine(String.Format("|{0,-10}|{1,-8}|{2,-12}|{3,-66}|{4,-10}|{5,9}|", 
                                i.StockTransactionID, i.PartID,i.DateStamp.Value.ToShortDateString(),StringTool.Truncate( i.Description,62), i.TransActionType, i.Qnty));
                        }
                        decimal inventoryCount = ctx.Inventory.Where(l => l.PartID == partID).Sum(c => c.Qnty);
                        sb.AppendLine(Filler(120)); //--
                        sb.AppendLine(" ");
                        sb.Append("Current Stock".PadRight(28));
                        sb.AppendLine(inventoryCount.ToString());

                        

                    }
                    else  //THERE IS NO TRANSACTIONS SO MAKE A FIRST ONE
                    {

                        sb.AppendLine("No Transactions Found");



                    }

                }
                catch { sb.AppendLine("No Valid Part Found"); }

            }

            return sb.ToString();
        }

        public static string setlevel(int partID, decimal amountdesired)
        {

            StringBuilder sb = new StringBuilder();
            using (var ctx = new MyContext())
            {
                try
                {

                    var partTransactions = ctx.Inventory.Where(l => l.PartID == partID).ToList();
                    if (partTransactions.Count > 0)
                    {
                        sb.AppendLine("+ --------------------------------------------------------------------- +");
                        foreach (Inventory i in partTransactions)
                        {
                            sb.Append(i.StockTransactionID.ToString().PadRight(12));
                            sb.Append(i.PartID.ToString().PadRight(10));
                            sb.Append(i.Description.Trim());
                            sb.AppendLine(i.Qnty.ToString());
                        }
                        decimal inventoryCount = ctx.Inventory.Where(l => l.PartID == partID).Sum(c => c.Qnty);
                        if (inventoryCount > amountdesired)
                        {
                            decimal push = (Math.Abs(inventoryCount - amountdesired)) * -1.0m;
                            sb.Append("Current Stock".PadRight(20));
                            sb.AppendLine(inventoryCount.ToString());
                            sb.Append("Difference".PadRight(20));
                            sb.AppendLine(push.ToString());
                            transaction(partID, push, 4);
                            sb.AppendLine("Pull this amount-".PadRight(20) + push.ToString());
                            sb.AppendLine("+ --------------------------------------------------------------------- +");
                        }
                        else if (inventoryCount > amountdesired)
                        {
                            decimal push = (Math.Abs(inventoryCount - amountdesired));
                            sb.Append("Current Stock".PadRight(20));
                            sb.AppendLine(inventoryCount.ToString());
                            sb.Append("Difference".PadRight(20));
                            sb.AppendLine(push.ToString());
                            transaction(partID, push, 4);
                            sb.AppendLine("Push this amount-".PadRight(20) + push.ToString());
                            sb.AppendLine("+ --------------------------------------------------------------------- +");
                        }
                    }
                    else  //THERE IS NO TRANSACTIONS SO MAKE A FIRST ONE
                    {
                        transaction(partID, amountdesired, 2);
                        sb.AppendLine("----------------------------------------------------------");
                        sb.AppendLine("Current Level");

                        stocklevel(partID);
                    }

                }
                catch { sb.AppendLine("No Valid Part Found"); }

            }

            return sb.ToString();
        }

        static string transaction(int id, decimal amount, int tcode = 0)
        {

            StringBuilder sb = new StringBuilder();
            using (var ctx = new MyContext())
            {

                try
                {
                    //Retrieve the part instance
                    Part p = ctx.Part.Where(c => c.PartID == id).Single();
                    Inventory inventory = new Inventory();
                    inventory.PartID = p.PartID;
                    inventory.Qnty = amount;
                    inventory.TransActionType = 4;
                    inventory.DateStamp = DateTime.Today;
                    inventory.Description = p.ItemDescription;
                    inventory.UnitOfMeasure = p.UID.Value;

                    ctx.Inventory.Add(inventory);
                    ctx.SaveChanges();

                    decimal inventoryCount = ctx.Inventory.Where(l => l.PartID == id).Sum(c => c.Qnty );

                    sb.AppendLine(String.Empty);
                    sb.AppendLine(amount.ToString() + "-Part Pushed To Inventory");
                    sb.Append("Current Stock Level -- ");
                    sb.AppendLine(inventoryCount.ToString());
                    if (p.UID.HasValue)
                    {
                        UnitOfMeasure uom = ctx.UnitOfMeasure.Where(u => u.UID == p.UID.Value).Single();
                        sb.Append("Unit -> ");
                        sb.AppendLine(uom.UOM);

                    }

                }
                catch { sb.AppendLine("No Valid Part Found"); }

            }

            return sb.ToString();
        }

        public static string pushpart(int id, decimal amount, int jobnum = 0)
        {

            StringBuilder sb = new StringBuilder();
            using (var ctx = new MyContext())
            {

                try
                {
                    //Retrieve the part instance
                    Part p = ctx.Part.Where(c => c.PartID == id).Single();
                    Inventory inventory = new Inventory();
                    inventory.PartID = p.PartID;
                    inventory.Qnty = amount;
                    inventory.TransActionType = 2;
                    inventory.DateStamp = DateTime.Today;
                    inventory.Description = p.ItemDescription;
                    inventory.UnitOfMeasure = p.UID;
                    if (jobnum != 0)
                    {
                        inventory.JobID = jobnum;
                    }
                    ctx.Inventory.Add(inventory);
                    ctx.SaveChanges();


                    decimal inventoryCount = ctx.Inventory.Where(l => l.PartID == id).Sum(c => c.Qnty);

                    sb.AppendLine(String.Empty);
                    sb.AppendLine(amount.ToString() + "-Part Pushed To Inventory");
                    sb.Append("Current Stock Level -- ");
                    sb.AppendLine(inventoryCount.ToString());
                    if (p.UID.HasValue)
                    {
                        UnitOfMeasure uom = ctx.UnitOfMeasure.Where(u => u.UID == p.UID.Value).Single();
                        sb.Append("Unit -> ");
                        sb.AppendLine(uom.UOM);

                    }

                }
                catch { sb.AppendLine("No Valid Part Found"); }

            }

            return sb.ToString();
        }

       

        public static string showrecent()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(Filler(120));
            sb.AppendLine(String.Format("|{0,-10}|{1,-8}|{2,-16}|{3,-64}|{4,-9}|{5,8}|", "Stock_ID", "Part_ID", "Date", "Description", "TransType", "Qnty")); //header--
            sb.AppendLine(Filler(120));
            using (var ctx = new MyContext())
            {

                try
                {
                    //Retrieve list of latest transacions
                    var p = ctx.Inventory.Where(i=> i.DateStamp > DateTime.Today.AddDays(-7)).ToList().OrderByDescending(d=> d.DateStamp);
                    int lineNumber = 0;
                    foreach (Inventory inv in p)
                    {

                        string transType;
                        lineNumber += 1;
                        string pid;
                        if (inv.PartID.HasValue) { pid = inv.PartID.Value.ToString(); } else { pid = "NA"; }
                        string desc = StringTool.Truncate(inv.Description.TrimEnd(), 60);
                        if (inv.Description.Trim().Length > 60) { desc += "..."; }
                       
                        
                        sb.AppendLine(String.Format("|{0,-10}|{1,-8}|{2,-16}|{3,-64}|{4,-9}|{5,8}|",inv.StockTransactionID.ToString(),
                            pid, inv.DateStamp.Value.ToShortDateString(), desc,inv.TransActionType, inv.Qnty.ToString())); 
                    }
                   
                 

                }
                catch { sb.AppendLine("No Transaction found"); }

            }

            return sb.ToString();

        }
       
        public static string pullpart(int id, decimal amount, int jobnum = 0)
        {

            StringBuilder sb = new StringBuilder();
            using (var ctx = new MyContext())
            {
                try
                {
                    //Retrieve the part instance
                    Part p = ctx.Part.Where(c => c.PartID == id).Single();
                    Inventory inventory = new Inventory();
                    inventory.PartID = p.PartID;
                    inventory.Qnty = -amount;
                    inventory.TransActionType = 3;
                    inventory.DateStamp = DateTime.Today;
                    inventory.Description = p.ItemDescription.Trim();
                    inventory.UnitOfMeasure = p.UID;
                    if (jobnum != 0)
                    {
                        inventory.JobID = jobnum;
                    }
                    ctx.Inventory.Add(inventory);
                    ctx.SaveChanges();


                    decimal inventoryCount = ctx.Inventory.Where(l => l.PartID == id).Sum(c => c.Qnty);
                    sb.AppendLine(String.Empty);
                    sb.AppendLine(amount.ToString() + "-Part Pulled From Inventory");
                    sb.Append("Current Stock Level -- ");
                    sb.AppendLine(inventoryCount.ToString());
                    if (p.UID.HasValue)
                    {
                        UnitOfMeasure uom = ctx.UnitOfMeasure.Where(u => u.UID == p.UID.Value).Single();
                        sb.Append("Unit -> ");
                        sb.AppendLine(uom.UOM);

                    }
                }
                catch { sb.AppendLine("No Valid Part Found"); }

            }

            return sb.ToString();
        }

        public static string Filler(int charactercount) {

            StringBuilder sb = new StringBuilder();
            sb.Append("#");
            for (int i = 0; i < charactercount; i++)
            {
                sb.Append("#");
            }
            sb.Append("#");
            return sb.ToString();
        }

        public static string FillDown(int charactercount)
        {

            StringBuilder sb = new StringBuilder();
            
            for (int i = 0; i < charactercount -1; i++)
            {
                sb.AppendLine("|");
            }
           
            return sb.ToString();
        }

        public static string pullstocktag(int id)
        {

            StringBuilder sb = new StringBuilder();
            using (var ctx = new MyContext())
            {
                try
                {
                    Inventory inv = ctx.Inventory.Include(p => p.OrderReciept).ThenInclude(o => o.PurchaseOrder).Where(c => c.LineID == id).FirstOrDefault();

                    Inventory inventoryItem = ctx.Inventory.Include(o => o.OrderReciept).Where(c => c.LineID == id).FirstOrDefault();
                    if(inventoryItem != null)
                    {
                        //sb.AppendLine("Found it!");
                        Inventory pushLine = new Inventory();
                        pushLine.DateStamp = DateTime.Today;
                        pushLine.Description = inventoryItem.Description;
                        pushLine.JobID = inventoryItem.JobID;
                        pushLine.LineID = inventoryItem.LineID;
                        pushLine.Location = inventoryItem.Location;
                        pushLine.Qnty = inventoryItem.Qnty * -1.0m ;
                        pushLine.TransActionType = 3;
                        pushLine.UnitOfMeasure = inventoryItem.UnitOfMeasure;
                        ctx.Inventory.Add(pushLine);
                        ctx.SaveChanges();
                        sb.AppendLine(String.Format("Line {0} pulled from inventory",pushLine.LineID));

                    }

               

                }
                catch { sb.AppendLine("No TagFound"); }
            }
            return sb.ToString();
        }

        public static void partify()
        {
            StringBuilder sb = new StringBuilder();
            Program.RunPartifyMenu();
        }

        public static string showdeadparts(string search)
        {

            StringBuilder sb = new StringBuilder();
            using (var ctx = new MyContext())
            {
                sb.AppendLine(Filler(120));
                sb.AppendLine(String.Format("|{0,-8}|{1,-10}|{2,-100}|",
                       "Part#", "DateAdded","Description"));
                sb.AppendLine(Filler(120));


                try
                {

                    var deadparts = ctx.DeadParts.Where(p=> p.ItemDescription.Contains(search)).ToList();
                    
                    if (deadparts.Count > 0)
                    {
                        foreach(DeadParts p in deadparts)
                        {
                            string trimmedDescription = StringTool.Truncate(p.ItemDescription.ToString().TrimEnd(), 95);
                            string added = p.DateAdded.HasValue ? p.DateAdded.Value.ToShortDateString() :"Na";
                            sb.AppendLine(String.Format("|{0,-8}|{1,-10}|{2,-100}|",p.PartID,added, p.ItemDescription));

                        }
                        
                      

                    }
                }
                catch { sb.AppendLine("No dead parts found"); }

            }

            return sb.ToString();

        }

        public static string deletepart(int id)
        {

            StringBuilder sb = new StringBuilder();
            using (var ctx = new MyContext())
            {
                sb.AppendLine(Filler(120));
                sb.AppendLine(String.Format("|{0,-8}|{1,-10}|{2,-100}|",
                       "Part#", "DateAdded", "Description"));
                sb.AppendLine(Filler(120));


                try
                {

                    var part = ctx.Part.Where(p => p.PartID == id).First();
                    Console.Write(String.Format("Delete Part {0}  y/n : ", part.ItemDescription));
                    string answer = Console.Read().ToString();
                    if(answer == "121")
                    {
                        ctx.Remove(part);
                        ctx.SaveChanges();
                        sb.Append(String.Format("Part {0} deleted", part.PartID));
                    }
                    
                }
                catch { sb.AppendLine("Part Delete Failed"); }

            }

            return sb.ToString();

        }

        public static string showstocktag(int id)
        {

            StringBuilder sb = new StringBuilder();
            using (var ctx = new MyContext())
            {
                try
                {


                    List<Inventory> inventoryItems = ctx.Inventory.Include(o => o.OrderReciept).
                        ThenInclude(p => p.PurchaseOrder).ThenInclude(e => e.Employee).
                        Where(c => c.LineID == id).ToList();
                    if (inventoryItems != null)
                    {
                        foreach (Inventory item in inventoryItems)
                        {

                        

                        string trimmedDescription = StringTool.Truncate(item.Description.ToString().TrimEnd(), 100);
                        sb.AppendLine("");
                        sb.AppendLine(String.Format("| Found       | {0,-105}|", trimmedDescription));
                        sb.AppendLine(String.Format("| Quantity    | {0,-105}|", item.Qnty.ToString()));
                        sb.AppendLine(String.Format("| Received    | {0,-105}|", item.DateStamp.Value.ToShortDateString()));
                        sb.AppendLine(String.Format("| Order Num   | {0,-105}|", item.OrderReciept.PurchaseOrder.OrderNum.ToString()));
                        sb.AppendLine(String.Format("| Order-Date  | {0,-105}|", item.OrderReciept.PurchaseOrder.OrderDate.Value.ToShortDateString()));
                        sb.AppendLine(String.Format("| Purchaser   | {0,-105}|", item.OrderReciept.PurchaseOrder.Employee.lastname));
                        var job = ctx.Job.Where(j => j.job_id == item.JobID.Value).First();
                        sb.AppendLine(String.Format("| Jobname     | {0,-105}|", job.jobName.ToString()));
                        }

                    }
                }
                catch { sb.AppendLine("No Valid Stock Tag Found"); }

            }

            return sb.ToString();

        }

        public static string findstocktag(int id)
        {

            StringBuilder sb = new StringBuilder();
            using (var ctx = new MyContext())
            {
                try
                {
                    

                    Inventory inventoryItem = ctx.Inventory.Include( o => o.OrderReciept).
                        ThenInclude(p => p.PurchaseOrder).ThenInclude(e=> e.Employee).
                        Where(c => c.LineID == id).FirstOrDefault();
                    if (inventoryItem != null)
                    {
                        string trimmedDescription = StringTool.Truncate(inventoryItem.Description.ToString().TrimEnd(),100);
                        sb.AppendLine("");
                        sb.AppendLine(String.Format("| Found       | {0,-105}|", trimmedDescription));
                        sb.AppendLine(String.Format("| Quantity    | {0,-105}|", inventoryItem.Qnty.ToString()));
                        sb.AppendLine(String.Format("| Received    | {0,-105}|", inventoryItem.DateStamp.Value.ToShortDateString()));                  
                        sb.AppendLine(String.Format("| Order Num   | {0,-105}|", inventoryItem.OrderReciept.PurchaseOrder.OrderNum.ToString()));
                        sb.AppendLine(String.Format("| Order-Date  | {0,-105}|", inventoryItem.OrderReciept.PurchaseOrder.OrderDate.Value.ToShortDateString()));
                        sb.AppendLine(String.Format("| Purchaser   | {0,-105}|", inventoryItem.OrderReciept.PurchaseOrder.Employee.lastname));
                        var job = ctx.Job.Where(j => j.job_id == inventoryItem.JobID.Value).First();
                        sb.AppendLine(String.Format("| Jobname     | {0,-105}|", job.jobName.ToString()));

                    }
                }
                catch { sb.AppendLine("No Valid Stock Tag Found"); }
                
            }
            
            return sb.ToString();
           
        }

        public static string supplierbuys(int id)
        {
            StringBuilder sb = new StringBuilder();
            using (var ctx = new MyContext())
            {
                sb.AppendLine(Filler(120));
                sb.AppendLine(String.Format("|{0,-10}|{1,-10}|{2,-12}|{3,-10}|{4,-74}|", "LineID", "Order#", "Date", "Qnty", "Discription"));
                sb.AppendLine(Filler(120));
                try
                {
                    string sql = String.Format("Select * FROM PurchaseLineItem where PurchaseOrderID IN (Select OrderNum From PurchaseOrder  Where SupplierID ={0})", id.ToString()); 

                    var lines = ctx.PurchaseLineItem.FromSql(sql).ToList();
                    int lineCount = 0;

                    foreach (PurchaseLineItem l in lines)
                    {

                        string desc;
                        if (l.Description.ToString().TrimEnd().Length > 70)
                        { desc = StringTool.Truncate(l.Description.ToString().TrimEnd(), 70) + "..."; }
                        else
                        { desc = l.Description.ToString().TrimEnd(); }

                        string qnty = String.Format("{0:0.00}", l.Qnty);
                        //string date = l.PurchaseOrder.OrderDate.Value.ToShortDateString();
                        sb.AppendLine(String.Format("|{0,-10}|{1,-10}|{2,-12}|{3,-10}|", l.LineID, l.PurchaseOrderID, l.Qnty.HasValue ?  l.Qnty : 0, desc));
                        lineCount++;
                    }

                    sb.Append(String.Format("{0} - Records Found", lineCount.ToString()));
                }
                catch (Exception ex)
                {
                    sb.AppendLine(ex.InnerException.ToString());

                }


            }

            return sb.ToString();
        }

        public static string unreceive(int id)
        {

            StringBuilder sb = new StringBuilder();
            using (var ctx = new MyContext())
            {
                try
                {
                    SqlParameter param1 = new SqlParameter("@ordnum", id);
                    param1.SqlDbType = System.Data.SqlDbType.Int;
                    int result = ctx.Database.ExecuteSqlCommand("unrecieve @ordnum", param1);
                    if(result > 0)
                    {
                        sb.Append(String.Format("Successfully Unreceived Order {0}", id.ToString()));
                    }
                }
                catch { sb.AppendLine("Error Unreceiving order " + id.ToString()); }

            }

            return sb.ToString();
        }

        public static string deleteorder(int id)
        {

            StringBuilder sb = new StringBuilder();
            using (var ctx = new MyContext())
            {
                try
                {
                    SqlParameter param1 = new SqlParameter("@OrderNum", id);
                    param1.SqlDbType = System.Data.SqlDbType.Int;
                    int result = ctx.Database.ExecuteSqlCommand("DeletePO  @OrderNum", param1);
                    if (result > 0)
                    {
                        sb.Append(String.Format("Successfully Deleted Order {0}", id.ToString()));
                    }
                }
                catch { sb.AppendLine("Error deleting order " + id.ToString()); }

            }

            return sb.ToString();
        }

        public static string stocklevel(int id)
        {

            StringBuilder sb = new StringBuilder();
            using (var ctx = new MyContext())
            {
                try
                {
                    Part p = ctx.Part.Where(c => c.PartID == id).Single();
                    sb.Append("PartID -> ");
                    sb.AppendLine(p.PartID.ToString());
                    sb.Append("Item Description ->");
                    sb.AppendLine(p.ItemDescription.Trim());

                    decimal inventoryCount = ctx.Inventory.Where(l => l.PartID == id).Sum(c => c.Qnty);
                    sb.Append(String.Empty);
                    sb.Append("Stock On Hand -- ");
                    sb.AppendLine(inventoryCount.ToString());
                    if (p.UID.HasValue)
                    {
                        UnitOfMeasure uom = ctx.UnitOfMeasure.Where(u => u.UID == p.UID.Value).Single();
                        sb.Append("Unit -> ");
                        sb.AppendLine(uom.UOM);

                    }

                }
                catch { sb.AppendLine("No Valid Part Found"); }

            }

            return sb.ToString();
        }

        public static void quit()
        {
            Environment.Exit(0);

        }

        public static string findpart(string src)
        {

            StringBuilder sb = new StringBuilder();
            using (var ctx = new MyContext())
            {
                try
                {
                    var found = ctx.Part.Where(c => c.ItemDescription.Contains(src)).ToList();
                    if (found.Count > 0)
                    {
                        foreach (Part p in found)
                        {
                            sb.Append(p.PartID.ToString().PadRight(20));
                            sb.AppendLine(p.ItemDescription);
                        }
                    }

                }
                catch { sb.AppendLine("No Valid Part Found"); }

            }

            return sb.ToString();
        }

        public static string findsupplier(string src = "0")
        {
            StringBuilder sb = new StringBuilder();
            using (var ctx = new MyContext())
            {
                try
                {
                    if (src != "0")
                    {
                        var found = ctx.Supplier.Where(c => c.SupplierName.Contains(src)).ToList();
                        if (found.Count > 0)
                        {
                            sb.AppendLine(" ");
                            sb.AppendLine(String.Format("|{0,-7}|{1,-55}|", "Supplier_ID", "SupplierName"));
                            sb.AppendLine("|-----------------------------------------------------------------------|");

                            foreach (Supplier p in found)
                            {
                                sb.AppendLine(String.Format("|{0,-7}|{1,-55}|", p.SupplierID, p.SupplierName));

                            }
                        }
                    }
                    else
                    {
                        var found = ctx.Supplier.ToList();
                        if (found.Count > 0)
                        {
                            sb.AppendLine(" ");
                            sb.AppendLine(String.Format("|{0,-7}|{1,-55}|", "Job_ID", "JobNum", "Job Name"));
                            sb.AppendLine("|-----------------------------------------------------------------------|");

                            foreach (Supplier p in found)
                            {
                                sb.AppendLine(String.Format("|{0,-7}|{1,-55}|", p.SupplierID, p.SupplierName));

                            }
                        }
                    }

                }
                catch { sb.AppendLine("No Valid Job Found"); }

            }

            return sb.ToString();


        }

        public static string findjob(string src = "0")
        {

            StringBuilder sb = new StringBuilder();
            using (var ctx = new MyContext())
            {
                try
                {
                    if (src != "0")
                    {
                        var found = ctx.Job.Where(c => c.jobName.Contains(src)).ToList();
                        if (found.Count > 0)
                        {
                            sb.AppendLine(" ");
                            sb.AppendLine(String.Format("|{0,-7}|{1,-7}|{2,-55}|", "Job_ID", "JobNum", "Job Name"));
                            sb.AppendLine("|-----------------------------------------------------------------------|");

                            foreach (Job p in found)
                            {
                                sb.AppendLine(String.Format("|{0,-7}|{1,-7}|{2,-55}|", p.job_id, p.jobnumber, p.jobName));

                            }
                        }
                    }
                    else
                    {
                        var found = ctx.Job.ToList();
                        if (found.Count > 0)
                        {
                            sb.AppendLine(" ");
                            sb.AppendLine(String.Format("|{0,-7}|{1,-7}|{2,-55}|", "Job_ID", "JobNum", "Job Name"));
                            sb.AppendLine("|-----------------------------------------------------------------------|");

                            foreach (Job p in found)
                            {
                                sb.AppendLine(String.Format("|{0,-7}|{1,-7}|{2,-55}|", p.job_id, p.jobnumber, p.jobName));

                            }
                        }
                    }

                }
                catch { sb.AppendLine("No Valid Job Found"); }

            }

            return sb.ToString();
        }

        public static string showinventory(string filter = "0")
        {

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("");
            sb.Append("|------------------------------------------------------------------------------------------------------------------------|");
           
            using (var ctx = new MyContext())
            {
                if(filter != "0")
                {
                    sb.AppendLine(" ");
                    sb.AppendLine(String.Format("|{0,-12}|{1,-90}|{2,16}|", "Part_ID", "Description", "Stock Available"));
                    sb.AppendLine("|------------------------------------------------------------------------------------------------------------------------|");
                    var inventory = ctx.Inventory.Where(c => c.Description.Contains(filter)).ToList();
                    foreach (Inventory i in inventory)
                    {
                        sb.AppendLine(String.Format("|{0,-12}|{1,-90}|{2,16}|",i.PartID, StringTool.Truncate(i.Description.TrimEnd(),90), i.Qnty));

                    }
                }
                else {
                  //var  inventory = ctx.Inventory.Where(c => c.PartID != null).ToList();
                  //  foreach (Inventory i in inventory)
                  //  {
                  //      sb.Append(i.PartID.ToString());
                  //      sb.Append("--");
                  //      if (i.Description != null) sb.Append(i.Description.Trim());
                  //      sb.Append("   ->  ");
                  //      sb.AppendLine(i.Qnty.ToString());

                  //  }
                }
               
            }
            
            sb.Append("|------------------------------------------------------------------------------------------------------------------------|");
            return sb.ToString();
        }

        public static string help()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("-----------Commands---------------");
            sb.AppendLine("");
            sb.AppendLine("findpart           <search string>");
            sb.AppendLine("showpart           <part number>");
            sb.AppendLine("stocklevel         <part number>");
            sb.AppendLine("pushpart           <part number, qnty>");
            sb.AppendLine("pullpart           <part number, qnty>, [optional-jobid]");
            sb.AppendLine("pullstocktag       <stocktagID>");
            sb.AppendLine("showdeadparts      <search string>");
            sb.AppendLine("deletepart         <part number> [y/N]");
            sb.AppendLine("setlevel           <part number, qnty>");
            sb.AppendLine("showtrans          <part number>");
            sb.AppendLine("showorder          <order number>");
            sb.AppendLine("showjoborders      <job_id , [optional-supplier name]");
            sb.AppendLine("showsupplierorders <supplier name");
            sb.AppendLine("showrecent         <displays one week of transactions");
            sb.AppendLine("findline           <search String>");
            sb.AppendLine("findstocktag       <tag number>");
            sb.AppendLine("showinventory      [optional-search string]");
            sb.AppendLine("findjob            [optional-search string]");
            sb.AppendLine("supplierbuys         <SupplierID> ");
            sb.AppendLine("findsupplier       [optional-search string]");
            sb.AppendLine("unreceive          <OrderNum>");
            sb.AppendLine("deleteorder        <OrderNum>");
            sb.AppendLine("quit");
            return sb.ToString();
        }

        private static void DisplayAppInformation()
        {
            string vers = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            Console.WriteLine(Mosiac.Commands.DefaultCommands.Filler(120));
            Console.WriteLine("Mosiac-Inventory".PadLeft(60));
            Console.WriteLine(String.Format("version {0,-24}".PadLeft(58), vers));
            Console.WriteLine(String.Format("Date    {0,-24}".PadLeft(58), DateTime.Today.ToShortDateString()));
            Console.WriteLine(Mosiac.Commands.DefaultCommands.Filler(120));
            Console.WriteLine(" ");
            Console.WriteLine(" ");

        }

        public static void clear()
        {
            try
            {
                Console.Clear();
                DisplayAppInformation();
            }
            catch (Exception)
            {

             
            }
          
        }



        public static string DoSomethingElse(DateTime date)
        {
            return string.Format(ConsoleFormatting.Indent(2) + "I did something else on {0}", date);
        }


        public static string DoSomethingOptional(int id, string data = "No Data Provided")
        {
            var result = string.Format(ConsoleFormatting.Indent(2) +
                "I did something to the record Id {0} and saved the data {1}", id, data);

            if (data == "No Data Provided")
            {
                result = string.Format(ConsoleFormatting.Indent(2) +
                "I did something to the record Id {0} but the optinal parameter "
                + "was not provided, so I saved the value '{1}'", id, data);
            }
            return result;
        }

        /// <summary>
        /// Custom string utility methods.
        /// </summary>
        public static class StringTool
        {
            /// <summary>
            /// Get a substring of the first N characters.
            /// </summary>
            public static string Truncate(string source, int length)
            {
                if (source.Length > length)
                {
                    source = source.Substring(0, length);
                }
                return source;
            }

            /// <summary>
            /// Get a substring of the first N characters. [Slow]
            /// </summary>
            public static string Truncate2(string source, int length)
            {
                return source.Substring(0, Math.Min(length, source.Length));
            }
        }
    }
}
