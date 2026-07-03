using CRMPortal.Models;
using Microsoft.EntityFrameworkCore;

namespace CRMPortal.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        
        public DbSet<Customers> Customers { get; set; }
        public DbSet<Users> Users { get; set; }
        public DbSet<Roles> Roles { get; set; }
        public DbSet<LeaveTypes> LeaveTypes { get; set; }
        public DbSet<LeaveRequests> LeaveRequests { get; set; }

        public DbSet<EmployeeFiles> EmployeeFiles { get; set; }
        public DbSet<MasterEmployee> MasterEmployee { get; set; }

    }
}
