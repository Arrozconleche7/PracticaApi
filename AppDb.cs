using Microsoft.EntityFrameworkCore;

namespace PracticaApi
{
    public class AppDb : DbContext
    {
        public AppDb(DbContextOptions<AppDb> opciones) : base(opciones) { }

        public DbSet<Model.Usuario> Usuarios => Set<Model.Usuario>();

        protected override void OnModelCreating(ModelBuilder mb)
        {
            //no se permiten dos cuentas con el mismo correo
            mb.Entity<Model.Usuario>().HasIndex(u => u.Correo).IsUnique();
        }
    }
}
