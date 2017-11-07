using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using PurchaseModel;


namespace Mosiac.Commands
{
    // Must be a public static class:
    public static class DefaultCommands
    {
        // Methods used as console commands must be public and must return a string

        public static string showpart(int id)
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
                    sb.AppendLine(p.Cost.ToString());

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
                        sb.AppendLine("+ --------------------------------------------------------------------- +");
                        foreach (Inventory i in partTransactions)
                        {
                            sb.Append(i.StockTransactionID.ToString().PadRight(8));
                            sb.Append(i.PartID.ToString().PadRight(8));
                            sb.Append(i.DateStamp.Value.ToShortDateString().PadRight(16));
                            sb.Append(i.Description.Trim());
                            sb.Append(i.TransActionType.ToString());
                            sb.AppendLine(i.Qnty.ToString());

                        }
                        decimal inventoryCount = ctx.Inventory.Where(l => l.PartID == partID).Sum(c => c.Qnty);
                        sb.AppendLine(" ");
                        sb.Append("Current Stock".PadRight(28));
                        sb.AppendLine(inventoryCount.ToString());

                        sb.AppendLine("+ --------------------------------------------------------------------- +");

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
                    inventory.UnitOfMeasure = p.UID;

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
            sb.Append("|");
            for (int i = 0; i < charactercount; i++)
            {
                sb.Append("-");
            }
            sb.Append("|");
            return sb.ToString();
        }

        public static string pullstocktag(int id)
        {

            StringBuilder sb = new StringBuilder();
            using (var ctx = new MyContext())
            {
                try
                {
                    Inventory inv = ctx.Inventory.Where(c => c.LineID == id).FirstOrDefault();

                    Inventory newInventory = new Inventory();

                    //sb.AppendLine(" ");
                    //sb.AppendLine(String.Format("|{0,-10}|{1,-10}|{2,-9}|{3,-80}|{4,-11}|", "StockTag", "PartID", "Qnty", "Description", "Recvd Date"));
                    //sb.AppendLine(Filler(125));
                    //foreach (Inventory i in inventory)
                    //{
                    //    sb.AppendLine(String.Format("|{0,-10}|{1,-10}|{2,-9}|{3,-80}|{4,-11:d}|", i.LineID, i.PartID.ToString() ?? "-", i.Qnty, StringTool.Truncate(i.Description.ToString().TrimEnd(), 80), i.DateStamp));
                    //}

                }
                catch { sb.AppendLine("No Valid Part Found"); }

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
                    var inventory = ctx.Inventory.Where(c => c.LineID == id).ToList();
                  
                    //if (p.UnitOfMeasure.HasValue)
                    //{
                    //    UnitOfMeasure uom = ctx.UnitOfMeasure.Where(u => u.UID == p.UnitOfMeasure.Value).Single();
                    //}


                    sb.AppendLine("||");
                    sb.AppendLine(" ");
                    sb.AppendLine(String.Format("|{0,-10}|{1,-10}|{2,-9}|{3,-80}|{4,-11}|","StockTag","PartID","Qnty", "Description", "Recvd Date"));
                    sb.AppendLine("|----------------------------------------------------------------------------------------------------------------------------|");
                    foreach (Inventory i in inventory)
                    {
                        sb.AppendLine(String.Format("|{0,-10}|{1,-10}|{2,-9}|{3,-80}|{4,-11:d}|", i.LineID, i.PartID.ToString() ?? "-",i.Qnty,StringTool.Truncate( i.Description.ToString().TrimEnd(),80), i.DateStamp));
                    }

                }
                catch { sb.AppendLine("No Valid Part Found"); }

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
                    sb.AppendLine(String.Format("|{0,-12}|{1,-90}|{2,-16}|", "Part_ID", "Description", "Stock Available"));
                    sb.AppendLine("|------------------------------------------------------------------------------------------------------------------------|");
                    var inventory = ctx.Inventory.Where(c => c.Description.Contains(filter)).ToList();
                    foreach (Inventory i in inventory)
                    {
                        sb.AppendLine(String.Format("|{0,-12}|{1,-90}|{2,-16}|",i.PartID, StringTool.Truncate(i.Description.TrimEnd(),90), i.Qnty));

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
            sb.AppendLine("findpart   <search string>");
            sb.AppendLine("showpart   <part number>");
            sb.AppendLine("stocklevel <part number>");
            sb.AppendLine("pushpart   <part number, qnty>");
            sb.AppendLine("pullpart   <part number, qnty>, [optional-jobid]");
            sb.AppendLine("setlevel   <part number, qnty>");
            sb.AppendLine("showtrans  <part number>");
            sb.AppendLine("showinventory [optional-search string]");
            sb.AppendLine("findjob       [optional-search string]");
            sb.AppendLine("quit");
            return sb.ToString();
        }


        //public static string FindPart(int id, string data)
        //{
        //    return string.Format(ConsoleFormatting.Indent(2) +
        //        "I did something to the record Id {0} and saved the data '{1}'", id, data);
        //}


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
