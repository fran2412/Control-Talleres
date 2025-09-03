using ControlTalleresMVP.Configuraciones;
using ControlTalleresMVP.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace ControlTalleresMVP.Persistence.DataContext
{
    public partial class EscuelaContext : DbContext
    {
        public EscuelaContext()
        {
        }

        public EscuelaContext(DbContextOptions<EscuelaContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Alumno> Alumnos { get; set; }
        public virtual DbSet<Sede> Sedes { get; set; }
        public virtual DbSet<Promotor> Promotores { get; set; }
        public virtual DbSet<Taller> Talleres { get; set; }
        public virtual DbSet<Inscripcion> Inscripciones { get; set; }
        public virtual DbSet<Generacion> Generaciones { get; set; }
        public virtual DbSet<Configuracion> Configuraciones { get; set; }
        public DbSet<Cargo> Cargos { get; set; } = null!;
        public DbSet<Pago> Pagos { get; set; } = null!;
        public DbSet<PagoAplicacion> PagoAplicaciones { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
                optionsBuilder.UseSqlite($"Data Source={AppPaths.DbPath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ====================
            // Entidad Alumno
            // ====================
            modelBuilder.Entity<Alumno>(entity =>
            {
                entity.HasKey(e => e.AlumnoId);

                entity.ToTable("alumnos");

                entity.HasIndex(e => new { e.Nombre });

                entity.Property(e => e.AlumnoId).HasColumnName("id_alumno");

                entity.Property(e => e.CreadoEn)
                    .HasColumnName("creado_en")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.ActualizadoEn)
                    .HasColumnName("actualizado_en")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.Eliminado)
                    .HasColumnName("eliminado")
                    .HasDefaultValue(false);

                entity.Property(e => e.EliminadoEn)
                    .HasColumnName("eliminado_en");

                entity.Property(e => e.Nombre).HasColumnName("nombre");

                entity.Property(e => e.Telefono).HasColumnName("telefono");

                // 🔹 FK a Sede
                entity.Property(e => e.SedeId).HasColumnName("id_sede");
                entity.HasOne(e => e.Sede)
                    .WithMany(s => s.Alumnos)
                    .HasForeignKey(e => e.SedeId)
                    .OnDelete(DeleteBehavior.Restrict);

                // 🔹 FK a Promotor
                entity.Property(e => e.PromotorId).HasColumnName("id_promotor");
                entity.HasOne(e => e.Promotor)
                    .WithMany(p => p.Alumnos)
                    .HasForeignKey(e => e.PromotorId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ====================
            // Entidad Sede
            // ====================
            modelBuilder.Entity<Sede>(entity =>
            {
                entity.HasKey(e => e.SedeId);
                entity.ToTable("sedes");

                entity.Property(e => e.CreadoEn)
                    .HasColumnName("creado_en")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.ActualizadoEn)
                    .HasColumnName("actualizado_en")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.Eliminado)
                    .HasColumnName("eliminado")
                    .HasDefaultValue(false);

                entity.Property(e => e.EliminadoEn)
                    .HasColumnName("eliminado_en");

                entity.Property(e => e.SedeId).HasColumnName("id_sede");
                entity.Property(e => e.Nombre).HasColumnName("nombre");
            });

            // ====================
            // Entidad Promotor
            // ====================
            modelBuilder.Entity<Promotor>(entity =>
            {
                entity.HasKey(e => e.PromotorId);
                entity.ToTable("promotores");

                entity.Property(e => e.PromotorId).HasColumnName("id_promotor");
                entity.Property(e => e.Nombre).HasColumnName("nombre");

                entity.Property(e => e.CreadoEn)
                    .HasColumnName("creado_en")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.ActualizadoEn)
                    .HasColumnName("actualizado_en")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.Eliminado)
                    .HasColumnName("eliminado")
                    .HasDefaultValue(false);

                entity.Property(e => e.EliminadoEn)
                    .HasColumnName("eliminado_en");
            });

            // ====================
            // Entidad Taller
            // ====================
            modelBuilder.Entity<Taller>(entity =>
            {
                entity.HasKey(e => e.TallerId);

                entity.ToTable("talleres");

                entity.Property(e => e.TallerId).HasColumnName("id_taller");
                entity.Property(e => e.Nombre).HasColumnName("nombre");
                entity.Property(e => e.Horario).HasColumnName("horario");

                entity.Property(e => e.CreadoEn)
                    .HasColumnName("creado_en")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.ActualizadoEn)
                    .HasColumnName("actualizado_en")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.Eliminado)
                    .HasColumnName("eliminado")
                    .HasDefaultValue(false);

                entity.Property(e => e.EliminadoEn).HasColumnName("eliminado_en");
            });

            // ====================
            // Entidad Generacion
            // ====================
            modelBuilder.Entity<Generacion>(entity =>
            {
                entity.HasKey(e => e.GeneracionId);

                entity.ToTable("generaciones");

                entity.Property(e => e.GeneracionId).HasColumnName("id_generacion");
                entity.Property(e => e.Nombre).HasColumnName("nombre");

                entity.Property(e => e.FechaInicio).HasColumnName("fecha_inicio");
                entity.Property(e => e.FechaFin).HasColumnName("fecha_fin");

                entity.Property(e => e.CreadoEn)
                    .HasColumnName("creado_en")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.ActualizadoEn)
                    .HasColumnName("actualizado_en")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.Eliminado)
                    .HasColumnName("eliminado")
                    .HasDefaultValue(false);

                entity.Property(e => e.EliminadoEn).HasColumnName("eliminado_en");
            });

            // ====================
            // Entidad Inscripcion
            // ====================
            modelBuilder.Entity<Inscripcion>(entity =>
            {
                entity.HasKey(i => i.InscripcionId);

                entity.ToTable("inscripciones");

                // Columnas (snake_case)
                entity.Property(i => i.InscripcionId).HasColumnName("id_inscripcion");

                entity.Property(i => i.Fecha)
                      .HasColumnName("fecha");

                entity.Property(i => i.Costo)
                      .HasColumnName("costo")
                      .HasPrecision(10, 2);

                entity.Property(i => i.SaldoActual)
                      .HasColumnName("saldo_actual")
                      .HasPrecision(10, 2);

                entity.Property(i => i.Estado)
                      .HasColumnName("estado")
                      .HasConversion<string>()
                      .HasMaxLength(20);

                entity.Property(i => i.CreadoEn)
                      .HasColumnName("creado_en")
                      .HasDefaultValueSql("CURRENT_TIMESTAMP")
                      .ValueGeneratedOnAdd();

                entity.Property(i => i.ActualizadoEn)
                      .HasColumnName("actualizado_en")
                      .HasDefaultValueSql("CURRENT_TIMESTAMP")
                      .ValueGeneratedOnAdd();

                entity.Property(i => i.Eliminado)
                      .HasColumnName("eliminado")
                      .HasDefaultValue(false);

                entity.Property(i => i.EliminadoEn)
                      .HasColumnName("eliminado_en");

                // FKs (snake_case)
                entity.Property(i => i.AlumnoId).HasColumnName("alumno_id");
                entity.Property(i => i.TallerId).HasColumnName("taller_id");
                entity.Property(i => i.GeneracionId).HasColumnName("generacion_id");

                // Relaciones
                entity.HasOne(i => i.Alumno)
                      .WithMany(a => a.Inscripciones)
                      .HasForeignKey(i => i.AlumnoId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(i => i.Taller)
                      .WithMany(t => t.Inscripciones)
                      .HasForeignKey(i => i.TallerId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(i => i.Generacion)
                      .WithMany(g => g.Inscripciones)
                      .HasForeignKey(i => i.GeneracionId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Índice único
                entity.HasIndex(i => new { i.AlumnoId, i.TallerId, i.GeneracionId, i.Eliminado })
                      .IsUnique();
            });

            // ====================
            // Entidad Configuracion
            // ====================
            modelBuilder.Entity<Configuracion>(entity =>
            {
                entity.ToTable("configuraciones");

                entity.HasKey(c => c.Clave);

                entity.Property(c => c.Clave).HasColumnName("clave");
                entity.Property(c => c.Valor).HasColumnName("valor");
                entity.Property(c => c.Descripcion).HasColumnName("descripcion");
            });

            // ====================
            // Entidad Cargo
            // ====================
            modelBuilder.Entity<Cargo>(entity =>
            {
                entity.HasKey(c => c.CargoId);

                entity.ToTable("cargos");

                entity.Property(c => c.CargoId).HasColumnName("id_cargo");

                entity.Property(c => c.Monto)
                      .HasColumnName("monto")
                      .HasPrecision(10, 2);

                entity.Property(c => c.SaldoActual)
                      .HasColumnName("saldo_actual")
                      .HasPrecision(10, 2);

                entity.Property(c => c.Tipo)
                      .HasColumnName("tipo")
                      .HasConversion<string>()
                      .HasMaxLength(50);

                entity.Property(c => c.Fecha)
                      .HasColumnName("fecha");

                entity.Property(c => c.CreadoEn)
                      .HasColumnName("creado_en")
                      .HasDefaultValueSql("CURRENT_TIMESTAMP")
                      .ValueGeneratedOnAdd();

                entity.Property(c => c.ActualizadoEn)
                      .HasColumnName("actualizado_en")
                      .HasDefaultValueSql("CURRENT_TIMESTAMP")
                      .ValueGeneratedOnAdd();

                entity.Property(c => c.Eliminado)
                      .HasColumnName("eliminado")
                      .HasDefaultValue(false);

                entity.Property(c => c.EliminadoEn)
                      .HasColumnName("eliminado_en");

                entity.Property(c => c.AlumnoId).HasColumnName("alumno_id");
                entity.Property(c => c.InscripcionId).HasColumnName("inscripcion_id");
                entity.Property(c => c.ClaseId).HasColumnName("clase_id");

                // Relaciones
                entity.HasOne(c => c.Alumno)
                      .WithMany(a => a.Cargos)
                      .HasForeignKey(c => c.AlumnoId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(c => c.Inscripcion)
                      .WithMany(i => i.Cargos)
                      .HasForeignKey(c => c.InscripcionId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(c => c.Aplicaciones)
                      .WithOne(pa => pa.Cargo)
                      .HasForeignKey(pa => pa.CargoId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ====================
            // Entidad Pago
            // ====================
            modelBuilder.Entity<Pago>(entity =>
            {
                entity.HasKey(p => p.PagoId);

                entity.ToTable("pagos");

                entity.Property(p => p.PagoId).HasColumnName("id_pago");

                entity.Property(p => p.Fecha)
                      .HasColumnName("fecha");

                entity.Property(p => p.MontoTotal)
                      .HasColumnName("monto_total")
                      .HasPrecision(10, 2);

                entity.Property(p => p.Metodo)
                      .HasColumnName("metodo")
                      .HasMaxLength(50);

                entity.Property(p => p.Referencia)
                      .HasColumnName("referencia");

                entity.Property(p => p.Notas)
                      .HasColumnName("notas");

                entity.Property(p => p.CreadoEn)
                      .HasColumnName("creado_en")
                      .HasDefaultValueSql("CURRENT_TIMESTAMP")
                      .ValueGeneratedOnAdd();

                entity.Property(p => p.ActualizadoEn)
                      .HasColumnName("actualizado_en")
                      .HasDefaultValueSql("CURRENT_TIMESTAMP")
                      .ValueGeneratedOnAdd();

                entity.Property(p => p.Eliminado)
                      .HasColumnName("eliminado")
                      .HasDefaultValue(false);

                entity.Property(p => p.EliminadoEn)
                      .HasColumnName("eliminado_en");

                entity.Property(p => p.AlumnoId).HasColumnName("alumno_id");

                // Relaciones
                entity.HasOne(p => p.Alumno)
                      .WithMany(a => a.Pagos)
                      .HasForeignKey(p => p.AlumnoId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(p => p.Aplicaciones)
                      .WithOne(pa => pa.Pago)
                      .HasForeignKey(pa => pa.PagoId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ====================
            // Entidad PagoAplicacion
            // ====================
            modelBuilder.Entity<PagoAplicacion>(entity =>
            {
                entity.HasKey(pa => pa.PagoAplicacionId);

                entity.ToTable("pago_aplicaciones");

                entity.Property(pa => pa.PagoAplicacionId).HasColumnName("id_pago_aplicacion");

                entity.Property(pa => pa.MontoAplicado)
                      .HasColumnName("monto_aplicado")
                      .HasPrecision(10, 2);

                entity.Property(pa => pa.PagoId).HasColumnName("pago_id");
                entity.Property(pa => pa.CargoId).HasColumnName("cargo_id");

                // Relaciones
                entity.HasOne(pa => pa.Pago)
                      .WithMany(p => p.Aplicaciones)
                      .HasForeignKey(pa => pa.PagoId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(pa => pa.Cargo)
                      .WithMany(c => c.Aplicaciones)
                      .HasForeignKey(pa => pa.CargoId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}