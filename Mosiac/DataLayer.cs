using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
namespace Mosiac
{
    public class InventoryContext: DbContext
    {


        public InventoryContext(DbContextOptions<InventoryContext> options) :base(options)
        {

        }

        
    }
}
