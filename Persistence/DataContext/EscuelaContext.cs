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
                entity.HasKey(e => e.IdAlumno);

                entity.ToTable("alumnos");

                entity.HasIndex(e => new { e.Nombre }, "idx_alumnos_nombre");

                entity.Property(e => e.IdAlumno).HasColumnName("id_alumno");

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
                entity.Property(e => e.IdSede).HasColumnName("id_sede");
                entity.HasOne(e => e.Sede)
                    .WithMany(s => s.Alumnos)
                    .HasForeignKey(e => e.IdSede)
                    .OnDelete(DeleteBehavior.Restrict);

                // 🔹 FK a Promotor
                entity.Property(e => e.IdPromotor).HasColumnName("id_promotor");
                entity.HasOne(e => e.Promotor)
                    .WithMany(p => p.Alumnos)
                    .HasForeignKey(e => e.IdPromotor)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Sede>(entity =>
            {
                entity.HasKey(e => e.IdSede);
                entity.ToTable("sedes");

                entity.Property(e => e.IdSede).HasColumnName("id_sede");
                entity.Property(e => e.Nombre).HasColumnName("nombre");
            });

            modelBuilder.Entity<Promotor>(entity =>
            {
                entity.HasKey(e => e.IdPromotor);
                entity.ToTable("promotores");

                entity.Property(e => e.IdPromotor).HasColumnName("id_promotor");
                entity.Property(e => e.Nombre).HasColumnName("nombre");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}