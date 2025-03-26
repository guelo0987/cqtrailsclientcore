using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using cqtrailsclientcore.Models;

namespace cqtrailsclientcore.Context;

public partial class MyDbContext : DbContext
{
    public MyDbContext()
    {
    }

    public MyDbContext(DbContextOptions<MyDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Ciudade> Ciudades { get; set; }

    public virtual DbSet<Empleado> Empleados { get; set; }

    public virtual DbSet<Empresa> Empresas { get; set; }

    public virtual DbSet<Notificacione> Notificaciones { get; set; }

    public virtual DbSet<Permiso> Permisos { get; set; }

    public virtual DbSet<PreFactura> PreFacturas { get; set; }

    public virtual DbSet<Reservacione> Reservaciones { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    public virtual DbSet<Vehiculo> Vehiculos { get; set; }

    public virtual DbSet<Carrito> Carrito { get; set; }

    public virtual DbSet<DetalleCarrito> DetalleCarrito { get; set; }

    public virtual DbSet<VehiculosReservacione> VehiculosReservaciones { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Ciudade>(entity =>
        {
            entity.HasKey(e => e.IdCiudad).HasName("Ciudades_pkey");

            entity.ToTable("Ciudades", "miguel");

            entity.Property(e => e.IdCiudad).UseIdentityAlwaysColumn();
            entity.Property(e => e.Estado).HasMaxLength(20);
            entity.Property(e => e.Nombre).HasMaxLength(20);
        });

        modelBuilder.Entity<Empleado>(entity =>
        {
            entity.HasKey(e => e.IdEmpleado).HasName("Empleados_pkey");

            entity.ToTable("Empleados", "miguel");

            entity.Property(e => e.IdEmpleado).UseIdentityAlwaysColumn();

            entity.HasOne(d => d.IdEmpresaNavigation).WithMany(p => p.Empleados)
                .HasForeignKey(d => d.IdEmpresa)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Empleados_IdEmpresa_fkey");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.Empleados)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Empleados_IdUsuario_fkey");
        });

        modelBuilder.Entity<Empresa>(entity =>
        {
            entity.HasKey(e => e.IdEmpresa).HasName("Empresas_pkey");

            entity.ToTable("Empresas", "miguel");

            entity.Property(e => e.IdEmpresa).UseIdentityAlwaysColumn();
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.ContactoEmail).HasMaxLength(100);
            entity.Property(e => e.ContactoTelefono).HasMaxLength(20);
            entity.Property(e => e.FechaRegistro)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.Nombre).HasMaxLength(20);
        });

        modelBuilder.Entity<Notificacione>(entity =>
        {
            entity.HasKey(e => e.IdNotificacion).HasName("Notificaciones_pkey");

            entity.ToTable("Notificaciones", "miguel");

            entity.Property(e => e.IdNotificacion).UseIdentityAlwaysColumn();
            entity.Property(e => e.FechaEnvio)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.TipoNotificacion).HasMaxLength(50);

            entity.HasOne(d => d.IdReservacionNavigation).WithMany(p => p.Notificaciones)
                .HasForeignKey(d => d.IdReservacion)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Notificaciones_IdReservacion_fkey");
        });

        modelBuilder.Entity<Permiso>(entity =>
        {
            entity.HasKey(e => e.IdPermiso).HasName("Permisos_pkey");

            entity.ToTable("Permisos", "miguel");

            entity.Property(e => e.IdPermiso).UseIdentityAlwaysColumn();
            entity.Property(e => e.Descripcion).HasMaxLength(100);
            entity.Property(e => e.NombrePermiso).HasMaxLength(20);
        });

        modelBuilder.Entity<PreFactura>(entity =>
        {
            entity.HasKey(e => e.IdPreFactura).HasName("PreFacturas_pkey");

            entity.ToTable("PreFacturas", "miguel");

            entity.Property(e => e.IdPreFactura).UseIdentityAlwaysColumn();
            entity.Property(e => e.ArchivoPdf)
                .HasMaxLength(255)
                .HasColumnName("ArchivoPDF");
            entity.Property(e => e.CostoAdicional)
                .HasPrecision(10, 2)
                .HasDefaultValueSql("0");
            entity.Property(e => e.CostoTotal).HasPrecision(10, 2);
            entity.Property(e => e.CostoVehiculo).HasPrecision(10, 2);
            entity.Property(e => e.FechaGeneracion)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");

            entity.HasOne(d => d.IdReservacionNavigation).WithMany(p => p.PreFacturas)
                .HasForeignKey(d => d.IdReservacion)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("PreFacturas_IdReservacion_fkey");
        });

        modelBuilder.Entity<Reservacione>(entity =>
        {
            entity.HasKey(e => e.IdReservacion).HasName("Reservaciones_pkey");

            entity.ToTable("Reservaciones", "miguel");

            entity.Property(e => e.IdReservacion).UseIdentityAlwaysColumn();
            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .HasDefaultValueSql("'Pendiente'::character varying");
            entity.Property(e => e.FechaConfirmacion).HasColumnType("timestamp without time zone");
            entity.Property(e => e.FechaFin).HasColumnType("timestamp without time zone");
            entity.Property(e => e.FechaInicio).HasColumnType("timestamp without time zone");
            entity.Property(e => e.FechaReservacion)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.RequerimientosAdicionales).HasMaxLength(255);
            entity.Property(e => e.RutaPersonalizada).HasMaxLength(255);

            entity.HasOne(d => d.IdEmpleadoNavigation).WithMany(p => p.Reservaciones)
                .HasForeignKey(d => d.IdEmpleado)
                .HasConstraintName("Reservaciones_IdEmpleado_fkey");

            entity.HasOne(d => d.IdEmpresaNavigation).WithMany(p => p.Reservaciones)
                .HasForeignKey(d => d.IdEmpresa)
                .HasConstraintName("Reservaciones_IdEmpresa_fkey");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.Reservaciones)
                .HasForeignKey(d => d.IdUsuario)
                .HasConstraintName("Reservaciones_IdUsuario_fkey");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.IdRol).HasName("Roles_pkey");

            entity.ToTable("Roles", "miguel");

            entity.Property(e => e.IdRol).UseIdentityAlwaysColumn();
            entity.Property(e => e.Descripcion).HasMaxLength(200);
            entity.Property(e => e.NombreRol).HasMaxLength(20);

            entity.HasMany(d => d.IdPermisos).WithMany(p => p.IdRols)
                .UsingEntity<Dictionary<string, object>>(
                    "RolesPermiso",
                    r => r.HasOne<Permiso>().WithMany()
                        .HasForeignKey("IdPermiso")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("RolesPermisos_IdPermiso_fkey"),
                    l => l.HasOne<Role>().WithMany()
                        .HasForeignKey("IdRol")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("RolesPermisos_IdRol_fkey"),
                    j =>
                    {
                        j.HasKey("IdRol", "IdPermiso").HasName("RolesPermisos_pkey");
                        j.ToTable("RolesPermisos", "miguel");
                    });
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.IdUsuario).HasName("Usuarios_pkey");

            entity.ToTable("Usuarios", "miguel");

            entity.HasIndex(e => e.Email, "Usuarios_Email_key").IsUnique();

            entity.Property(e => e.IdUsuario).UseIdentityAlwaysColumn();
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Apellido).HasMaxLength(30);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FechaRegistro)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.Nombre).HasMaxLength(20);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);

            entity.HasOne(d => d.IdRolNavigation).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.IdRol)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Usuarios_IdRol_fkey");
        });

        modelBuilder.Entity<Vehiculo>(entity =>
        {
            entity.HasKey(e => e.IdVehiculo).HasName("Vehiculos_pkey");

            entity.ToTable("Vehiculos", "miguel");

            entity.Property(e => e.IdVehiculo).UseIdentityAlwaysColumn();
            entity.Property(e => e.Disponible).HasDefaultValue(true);
            entity.Property(e => e.Modelo).HasMaxLength(50);
            entity.Property(e => e.Placa).HasMaxLength(20);
            entity.Property(e => e.TipoVehiculo).HasMaxLength(20);
        });

        modelBuilder.Entity<VehiculosReservacione>(entity =>
        {
            entity.HasKey(e => new { e.IdVehiculo, e.IdReservacion }).HasName("VehiculosReservaciones_pkey");

            entity.ToTable("VehiculosReservaciones", "miguel");

            entity.Property(e => e.EstadoAsignacion)
                .HasMaxLength(20)
                .HasDefaultValueSql("'Activa'::character varying");
            entity.Property(e => e.FechaAsignacion)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");

            entity.HasOne(d => d.IdReservacionNavigation).WithMany(p => p.VehiculosReservaciones)
                .HasForeignKey(d => d.IdReservacion)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("VehiculosReservaciones_IdReservacion_fkey");

            entity.HasOne(d => d.IdVehiculoNavigation).WithMany(p => p.VehiculosReservaciones)
                .HasForeignKey(d => d.IdVehiculo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("VehiculosReservaciones_IdVehiculo_fkey");
        });

        OnModelCreatingPartial(modelBuilder);

        modelBuilder.Entity<Carrito>()
            .HasKey(c => c.id); // Define CarritoId como clave primaria

        // Configuración de relaciones (opcional)
        modelBuilder.Entity<Carrito>()
            .HasOne(c => c.Usuario)
            .WithMany(u => u.Carritos)
            .HasForeignKey(c => c.usuario_id);

        modelBuilder.Entity<Carrito>()
            .ToTable("Carrito", "miguel");

        modelBuilder.Entity<DetalleCarrito>()
            .HasKey(d => d.id);

        modelBuilder.Entity<DetalleCarrito>()
            .HasOne(d => d.Carrito)
            .WithMany()
            .HasForeignKey(d => d.CarritoId);

        modelBuilder.Entity<DetalleCarrito>()
            .ToTable("DetalleCarrito", "miguel");
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
