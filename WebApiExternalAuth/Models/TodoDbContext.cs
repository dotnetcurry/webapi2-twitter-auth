using System;
using System.Data.Entity;

namespace WebApiExternalAuth.Models
{
    public class TodoDbContext : DbContext
    {
        public TodoDbContext()
            : base("DefaultConnection")
        {
        }

        public TodoDbContext(string nameOrConnectionString)
            : base(nameOrConnectionString)
        {
        }

        public DbSet<TodoItem> TodoItems { get; set; }
        public DbSet<TodoList> TodoLists { get; set; }
    }
}
