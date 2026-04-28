using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using LaMediaCancha.Models;

namespace LaMediaCancha.App_Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext() : base("name=DefaultConnection")
        {
            Database.SetInitializer<ApplicationDbContext>(null);
        }

        public DbSet<Empresa> Empresas { get; set; }
        public DbSet<Rol> Roles { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<EmpresaUsuario> EmpresaUsuarios { get; set; }
        public DbSet<Menu> Menus { get; set; }
        public DbSet<MenuRol> MenuRoles { get; set; }
        public DbSet<MenuUsuario> MenuUsuarios { get; set; }

        // Comenta o elimina esta línea - La Bitácora está en LaMediaCanchaDB
        // public DbSet<Bitacora> Bitacora { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
            modelBuilder.Conventions.Remove<OneToManyCascadeDeleteConvention>();

            // Usuario → Rol
            modelBuilder.Entity<Usuario>()
                .HasRequired(u => u.Rol)
                .WithMany(r => r.Usuarios)
                .HasForeignKey(u => u.RolId);

            // Usuario → Email único
            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // EmpresaUsuario → Empresa
            modelBuilder.Entity<EmpresaUsuario>()
                .HasRequired(eu => eu.Empresa)
                .WithMany(e => e.EmpresaUsuarios)
                .HasForeignKey(eu => eu.EmpresaId);

            // EmpresaUsuario → Usuario
            modelBuilder.Entity<EmpresaUsuario>()
                .HasRequired(eu => eu.Usuario)
                .WithMany(u => u.EmpresaUsuarios)
                .HasForeignKey(eu => eu.UsuarioId);

            // MenuRol → Menu
            modelBuilder.Entity<MenuRol>()
                .HasRequired(mr => mr.Menu)
                .WithMany(m => m.MenuRoles)
                .HasForeignKey(mr => mr.MenuId);

            // MenuRol → Rol
            modelBuilder.Entity<MenuRol>()
                .HasRequired(mr => mr.Rol)
                .WithMany(r => r.MenuRoles)
                .HasForeignKey(mr => mr.RolId);

            // MenuUsuario → Menu
            modelBuilder.Entity<MenuUsuario>()
                .HasRequired(mu => mu.Menu)
                .WithMany(m => m.MenuUsuarios)
                .HasForeignKey(mu => mu.MenuId);

            // MenuUsuario → Usuario
            modelBuilder.Entity<MenuUsuario>()
                .HasRequired(mu => mu.Usuario)
                .WithMany(u => u.MenuUsuarios)
                .HasForeignKey(mu => mu.UsuarioId);

            // Comenta o elimina estas configuraciones de Bitácora
            // Bitacora → Usuario (opcional)
            // modelBuilder.Entity<Bitacora>()
            //     .HasOptional(b => b.Usuario)
            //     .WithMany(u => u.Bitacoras)
            //     .HasForeignKey(b => b.UsuarioId);

            // Bitacora → Empresa (opcional)
            // modelBuilder.Entity<Bitacora>()
            //     .HasOptional(b => b.Empresa)
            //     .WithMany(e => e.Bitacoras)
            //     .HasForeignKey(b => b.EmpresaId);

            // Menu → MenuPadre (autorreferencia)
            modelBuilder.Entity<Menu>()
                .HasOptional(m => m.MenuPadre)
                .WithMany(m => m.SubMenus)
                .HasForeignKey(m => m.MenuPadreId);
        }
    }
}