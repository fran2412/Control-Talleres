using ControlTalleresMVP.Configuraciones;
using ControlTalleresMVP.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

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

                entity.HasIndex(e => new { e.Nombre }, "idx_alumnos_nombre");

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

            modelBuilder.Entity<Inscripcion>()
                    .HasOne(i => i.Alumno)
                    .WithMany(a => a.Inscripciones)
                    .HasForeignKey(i => i.AlumnoId);

            modelBuilder.Entity<Inscripcion>()
                    .HasOne(i => i.Taller)
                    .WithMany(t => t.Inscripciones)
                    .HasForeignKey(i => i.TallerId);

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
            // Entidad Inscripcion
            // ====================
            modelBuilder.Entity<Inscripcion>(entity =>
            {
                entity.HasKey(e => e.InscripcionId);

                entity.ToTable("inscripciones");

                entity.Property(e => e.InscripcionId).HasColumnName("id_inscripcion");
                entity.Property(e => e.Fecha).HasColumnName("fecha");
                entity.Property(e => e.Costo).HasColumnName("costo");

                // 🔹 FK a Alumno
                entity.Property(e => e.AlumnoId).HasColumnName("id_alumno");
                entity.HasOne(i => i.Alumno)
                    .WithMany(a => a.Inscripciones)
                    .HasForeignKey(i => i.AlumnoId)
                    .OnDelete(DeleteBehavior.Restrict);

                // 🔹 FK a Taller
                entity.Property(e => e.TallerId).HasColumnName("id_taller");
                entity.HasOne(i => i.Taller)
                    .WithMany(t => t.Inscripciones)
                    .HasForeignKey(i => i.TallerId)
                    .OnDelete(DeleteBehavior.Restrict);

                // 🔹 FK a Generacion
                entity.Property(e => e.GeneracionId).HasColumnName("id_generacion");
                entity.HasOne(i => i.Generacion)
                    .WithMany(g => g.Inscripciones)
                    .HasForeignKey(i => i.GeneracionId)
                    .OnDelete(DeleteBehavior.Restrict);
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

            OnModelCreatingPartial(modelBuilder);
        }



        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}