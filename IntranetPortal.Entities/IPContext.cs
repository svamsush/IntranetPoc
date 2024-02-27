using IntranetPortal.Entities.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography.X509Certificates;

namespace IntranetPortal.Entities
{
    public partial class IPContext :DbContext
    {
        public IPContext()
        {

        }
        public IPContext(DbContextOptions<IPContext> options) : base(options)
        {
            
        }
        public virtual DbSet<Employee> Employees { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
