using Microsoft.EntityFrameworkCore;

namespace DocumentManagement.API.Context
{
    public class DocumentManagementDbContext : DbContext
    {
        public DocumentManagementDbContext(DbContextOptions<DocumentManagementDbContext> options) : base(options)
        {
        }
    }
}
