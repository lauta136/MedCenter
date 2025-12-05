using System;
using System.Collections.Generic;
using MedCenter.Models;
using Microsoft.EntityFrameworkCore;
using MedCenter.Services.Authentication.Components;

namespace MedCenter.Data;

public partial class AppDbContext : DbContext
{
    private readonly IConfiguration _configuration;

    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options, IConfiguration configuration)
        : base(options)
    {
        _configuration = configuration;
    }

    public virtual DbSet<EntradaClinica> entradasclinicas { get; set; }

    public virtual DbSet<Especialidad> especialidades { get; set; }

    public virtual DbSet<HistoriaClinica> historiasclinicas { get; set; }

    public virtual DbSet<Medico> medicos { get; set; }

    public virtual DbSet<Paciente> pacientes { get; set; }

    public virtual DbSet<Persona> personas { get; set; }

    public virtual DbSet<ReporteEstadistico> reportesestadisticos { get; set; }

    public virtual DbSet<Secretaria> secretarias { get; set; }

    public virtual DbSet<SlotAgenda> slotsagenda { get; set; }

    public virtual DbSet<Turno> turnos { get; set; }

    public virtual DbSet<MedicoEspecialidad> medicoEspecialidades { get; set; }
    public DbSet<RoleKey> role_keys { get; set; }
    public DbSet<ObraSocial> obras_sociales { get; set; }
    public DbSet<MedicoObraSocial> medico_obrasocial { get; set; }
    public DbSet<PacienteObraSocial> paciente_obrasocial { get; set; }
    public DbSet<TurnoAuditoria> turnoAuditorias { get; set; }
    public DbSet<DisponibilidadMedico> disponibilidad_medico { get; set; }
    public DbSet<TrazabilidadTurno> trazabilidadTurnos { get; set; }
    public DbSet<TrazabilidadLogin> trazabilidadLogins{get;set;}
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EntradaClinica>(entity =>
        {
            entity.HasKey(e => e.id).HasName("entradasclinicas_pkey");

            entity.Property(e => e.id).UseIdentityAlwaysColumn();
            entity.Property(e => e.diagnostico).HasMaxLength(255);
            entity.Property(e => e.observaciones).HasMaxLength(255);
            entity.Property(e => e.tratamiento).HasMaxLength(255);

            entity.HasOne(d => d.historia).WithMany(p => p.EntradasClinicas)
                .HasForeignKey(d => d.historia_id)
                .HasConstraintName("entradasclinicas_historia_id_fkey");

            entity.HasOne(d => d.medico).WithMany(p => p.entradasClinicas)
                .HasForeignKey(d => d.medico_id)
                .HasConstraintName("entradasclinicas_medico_id_fkey");

            
        });

        modelBuilder.Entity<Especialidad>(entity =>
        {
            entity.HasKey(e => e.id).HasName("especialidades_pkey");

            entity.Property(e => e.id).UseIdentityAlwaysColumn();
            entity.Property(e => e.nombre).HasMaxLength(50);
        });

        modelBuilder.Entity<HistoriaClinica>(entity =>
        {
            entity.HasKey(e => e.id).HasName("historiasclinicas_pkey");

            entity.HasIndex(e => e.paciente_id, "historiasclinicas_paciente_id_key").IsUnique();

            entity.Property(e => e.id).UseIdentityAlwaysColumn();

            entity.HasOne(d => d.paciente).WithOne(p => p.historiasclinicas)
                .HasForeignKey<HistoriaClinica>(d => d.paciente_id)
                .HasConstraintName("historiasclinicas_paciente_id_fkey");
        });

        modelBuilder.Entity<Medico>(entity =>
        {
            entity.HasKey(e => e.id).HasName("medicos_pkey");

            entity.Property(e => e.id).ValueGeneratedNever();
            entity.Property(e => e.matricula).HasMaxLength(20);

            entity.HasOne(d => d.idNavigation).WithOne(p => p.Medico)
                .HasForeignKey<Medico>(d => d.id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("medicos_id_fkey");

            entity.HasMany(o => o.medicosObraSociales).WithOne(o => o.medico)
                  .HasForeignKey(o => o.medico_id)
                  .OnDelete(DeleteBehavior.Restrict)
                  .HasConstraintName("medico_obrasocial_medico_fkey");
        });

        modelBuilder.Entity<Paciente>(entity =>
        {
            entity.HasKey(e => e.id).HasName("pacientes_pkey");

            entity.Property(e => e.id).ValueGeneratedNever();
            entity.Property(e => e.dni).HasMaxLength(20);
            entity.Property(e => e.telefono).HasMaxLength(30);

            entity.HasOne(d => d.idNavigation).WithOne(p => p.Paciente)
                .HasForeignKey<Paciente>(d => d.id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("pacientes_id_fkey");

            entity.HasMany(p => p.pacientesObrasSociales)
                  .WithOne(po => po.Paciente)
                  .HasForeignKey(po => po.paciente_id)
                  .HasConstraintName("paciente_obrasocial_paciente_fkey")
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Persona>(entity =>
        {
            entity.HasKey(e => e.id).HasName("personas_pkey");

            entity.Property(e => e.id).UseIdentityAlwaysColumn();
            entity.Property(e => e.contraseña).HasMaxLength(50);
            entity.Property(e => e.email).HasMaxLength(100);
            entity.Property(e => e.nombre).HasMaxLength(100);
        });

        modelBuilder.Entity<ReporteEstadistico>(entity =>
        {
            entity.HasKey(e => e.id).HasName("reportesestadisticos_pkey");

            entity.Property(e => e.id).UseIdentityAlwaysColumn();
            entity.Property(e => e.tipo).HasMaxLength(50);

            entity.HasOne(d => d.consultor).WithMany(p => p.reportesestadisticos)
                .HasForeignKey(d => d.consultor_id)
                .HasConstraintName("reportesestadisticos_consultor_id_fkey");
        });

        modelBuilder.Entity<Secretaria>(entity =>
        {
            entity.HasKey(e => e.id).HasName("secretarias_pkey");

            entity.Property(e => e.id).ValueGeneratedNever();
            entity.Property(e => e.legajo).HasMaxLength(20);

            entity.HasOne(d => d.idNavigation).WithOne(p => p.Secretaria)
                .HasForeignKey<Secretaria>(d => d.id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("secretarias_id_fkey");
        });

        modelBuilder.Entity<SlotAgenda>(entity =>
        {
            entity.HasKey(e => e.id).HasName("slotsagenda_pkey");

            entity.Property(e => e.id).UseIdentityAlwaysColumn();

            entity.HasOne(d => d.medico).WithMany(p => p.slotsAgenda)
                .HasForeignKey(d => d.medico_id)
                .HasConstraintName("slotsagenda_medico_id_fkey");
        });

        modelBuilder.Entity<Turno>(entity =>
        {
            entity.HasKey(e => e.id).HasName("turnos_pkey");

            entity.Property(e => e.id).UseIdentityAlwaysColumn();
            entity.Property(e => e.estado).HasMaxLength(20);
            entity.Property(e => e.motivo_cancelacion).HasMaxLength(255);

            entity.HasOne(d => d.medico).WithMany(p => p.turnos)
                .HasForeignKey(d => d.medico_id)
                .HasConstraintName("turnos_medico_id_fkey");

            entity.HasOne(d => d.paciente).WithMany(p => p.turnos)
                .HasForeignKey(d => d.paciente_id)
                .HasConstraintName("turnos_paciente_id_fkey")
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.especialidad).WithMany(p => p.turnos)
           .HasForeignKey(d => d.especialidad_id)
           .HasConstraintName("turnos_especialidad_id_fkey"); // Es una buena práctica darle un nombre

            entity.HasOne(d => d.secretaria).WithMany(p => p.turnos)
                .HasForeignKey(d => d.secretaria_id)
                .HasConstraintName("turnos_secretaria_id_fkey");

            entity.HasOne(t => t.slot).WithOne(s => s.Turno).HasForeignKey<Turno>(s => s.slot_id).HasConstraintName("turno_slotagenda_fkey");

            entity.HasOne(t => t.EntradaClinica).WithOne(ec => ec.turno).HasForeignKey<Turno>(t => t.entradaClinica_id).HasConstraintName("turno_entradaclinica_fkey");

            entity.HasOne(t => t.paciente_obrasocial)
                  .WithMany(po => po.turnos)
                  .HasForeignKey(t => t.pacienteobrasocial_id)
                  .HasConstraintName("turno_paciente_obrasocial_fkey")
                  .OnDelete(DeleteBehavior.Restrict);

        });

        modelBuilder.Entity<MedicoEspecialidad>(entity =>
        {
            // Le decimos el nombre correcto de la tabla en la base de datos
            entity.ToTable("medico_especialidad");

            // Definimos la clave primaria compuesta
            entity.HasKey(me => new { me.medicoId, me.especialidadId });

            // 3. ✅ MAPEAMOS LOS NOMBRES DE LAS COLUMNAS
            entity.Property(me => me.medicoId).HasColumnName("medico_id");
            entity.Property(me => me.especialidadId).HasColumnName("especialidad_id");

            // Configuramos la relación con Medico
            entity.HasOne(me => me.medico)
            .WithMany(m => m.medicoEspecialidades)
            .HasForeignKey(me => me.medicoId);

            // Configuramos la relación con Especialidad
            entity.HasOne(me => me.especialidad)
            .WithMany(e => e.medicoEspecialidades)
            .HasForeignKey(me => me.especialidadId);
        });

        modelBuilder.Entity<RoleKey>(entity =>
        {
            entity.ToTable("role_keys");
            entity.HasKey(r => r.Id);
            entity.Property(e => e.Id).UseIdentityAlwaysColumn();
            entity.Property(r => r.HashedKey).HasColumnName("hashed_key");
            entity.Property(r => r.Role).HasColumnName("role");
        });

        // Sembrar datos después de configurar la entidad
        var passwordHasher = new PasswordHashService();
        var medicoPlainKey = _configuration["RoleKeys:Medico"];
        var secretariaPlainKey = _configuration["RoleKeys:Secretaria"];

        var medicoKey = passwordHasher.HashPassword(medicoPlainKey);
        var secretariaKey = passwordHasher.HashPassword(secretariaPlainKey);

        modelBuilder.Entity<ObraSocial>(entity =>
        {
            entity.ToTable("obrassociales");

            entity.HasKey(os => os.id).HasName("obra_social_pkey");
            entity.Property(os => os.id).UseIdentityAlwaysColumn();

            entity.Property(os => os.nombre).HasMaxLength(100).IsRequired();
            entity.Property(os => os.sigla).HasMaxLength(20);
            entity.Property(os => os.activa).HasDefaultValue(true); //por defecto en la creacion esta activa

            entity.HasMany(os => os.pacientesObrasSociales)
                  .WithOne(po => po.obrasocial)
                  .HasForeignKey(po => po.obrasocial_id)
                  .HasConstraintName("paciente_obrasocial_obrasocial_fkey")
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(os => os.medicosObrasSociales)
                  .WithOne(mo => mo.obrasocial)
                  .HasForeignKey(mo => mo.obrasocial_id)
                  .HasConstraintName("medico_obrasocial_obrasocial_fkey")
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PacienteObraSocial>(entity =>
        {
            entity.ToTable("paciente_obrasocial");

            entity.HasKey(po => po.id);
            entity.Property(po => po.id).UseIdentityAlwaysColumn();

            entity.Property(po => po.numeroAfiliado).HasMaxLength(50).IsRequired();
            entity.Property(po => po.fecha_afiliacion).IsRequired();
            entity.Property(po => po.activa).HasDefaultValue(true);
            entity.Property(po => po.plan).HasMaxLength(100);

            entity.HasIndex(po => new
            {
                po.paciente_id,
                po.obrasocial_id,
                po.activa

            }).IsUnique().HasFilter("activa = TRUE");

        });

        modelBuilder.Entity<MedicoObraSocial>(entity =>
        {
            entity.ToTable("medico_obrasocial");

            entity.HasKey(mo => mo.id);
            entity.Property(mo => mo.id).UseIdentityAlwaysColumn();

            entity.Property(mo => mo.fecha_desde).IsRequired();
            entity.Property(mo => mo.activo).HasDefaultValue(true);

            entity.HasIndex(mo => new
            {
                mo.medico_id,
                mo.obrasocial_id,
                mo.activo
            }).IsUnique().HasFilter("activo = true");
        });

        modelBuilder.Entity<DisponibilidadMedico>(e =>
        {
            e.ToTable("disponibilidadesmedico");

            e.HasKey(e => e.id);
            e.Property(e => e.id).UseIdentityAlwaysColumn();

            e.HasOne(e => e.medico).WithMany(m => m.disponibilidadesMedico)
            .HasForeignKey(e => e.medico_id).HasConstraintName("disponibilidades_medico_fkey");

            e.Property(e => e.duracion_turno_minutos).HasDefaultValue(30);
            e.Property(e => e.activa).HasDefaultValue(true);

            e.HasMany(e => e.slotsAgenda).WithOne(sa => sa.bloqueDisponibilidad)
                                         .HasForeignKey(sa => sa.bloqueDisponibilidadId)
                                         .OnDelete(DeleteBehavior.Restrict)
                                         .HasConstraintName("slotsagenda_disponibilidadmedico_fkey");

            e.HasIndex(e => new { e.dia_semana, e.hora_inicio, e.hora_fin, e.medico_id, e.activa })
             .HasFilter("activa = true")
             .IsUnique();
        });

        modelBuilder.Entity<TurnoAuditoria>(e =>
        {
            e.ToTable("auditoriasturnos");

            e.HasKey(e => e.Id);
            e.Property(e => e.Id).UseIdentityAlwaysColumn().HasColumnName("id");

            e.Property(e => e.UsuarioNombre).HasColumnName("usuario_nombre").IsRequired();
            e.Property(e => e.PacienteDNI).HasColumnName("paciente_dni");
            e.Property(e => e.PacienteNombre). HasColumnName("paciente_nombre");
            e.Property(e => e.MedicoNombre). HasColumnName("medico_nombre");
            e.Property(e => e.TurnoId).HasColumnName("turno_id").IsRequired();
            e.Property(e => e.EstadoAnterior).HasColumnName("estado_anterior").HasConversion<string>();
            e.Property(e => e.EstadoNuevo).HasColumnName("estado_nuevo").HasConversion<string>();
            e.Property(e => e.FechaAnterior).HasColumnName("fecha_anterior");
            e.Property(e => e.FechaNueva).HasColumnName("fecha_nueva");
            e.Property(e => e.HoraAnterior).HasColumnName("hora_anterior");
            e.Property(e => e.HoraNueva).HasColumnName("hora_nueva");
            e.Property(e => e.MomentoAccion).HasColumnName("momento_accion");
            e.Property(e => e.SlotIdAnterior).HasColumnName("slot_id_anterior");
            e.Property(e => e.SlotIdNuevo).HasColumnName("slot_id_nuevo");
            e.Property(e => e.Accion).HasColumnName("accion").IsRequired().HasConversion<string>();
            e.Property(e => e.MotivoCancelacion).HasColumnName("motivo_cancelacion");

        });

        modelBuilder.Entity<TrazabilidadTurno>(e =>
        {
            e.ToTable("trazabilidadturnos");

            e.HasKey(e => e.Id);
            e.Property(e => e.Id).UseIdentityAlwaysColumn();

            e.Property(e => e.TurnoId).HasColumnName("turno_id");
            e.Property(e => e.UsuarioId).HasColumnName("usuario_id");
            e.Property(e => e.UsuarioNombre).HasColumnName("usuario_nombre");
            e.Property(e => e.UsuarioRol).HasColumnName("usuario_rol").HasConversion<string>();

            e.Property(e => e.Accion).HasColumnName("accion").HasConversion<string>();
            e.Property(e => e.Descripcion).HasColumnName("descripcion");
            e.Property(e => e.MomentoAccion).HasColumnName("momento_accion");

        });
        modelBuilder.Entity<TrazabilidadLogin>(e =>
        {
            e.ToTable("trazabilidadlogin");

            e.HasKey(e => e.Id);
            e.Property(e => e.Id).UseIdentityAlwaysColumn();

            e.Property(e => e.UsuarioRol).HasColumnName("usuario_rol").HasConversion<string>();
            e.Property(e => e.MomentoLogout).HasColumnName("momento_logout");
            e.Property(e => e.UsuarioId).HasColumnName("usuario_id");
            e.Property(e => e.UsuarioNombre).HasColumnName("usuario_nombre");
            e.Property(e => e.MomentoLogin).HasColumnName("momento_login");
            e.Property(e => e.TipoLogout).HasColumnName("tipo_logout").HasConversion<string>();
        });
            
        

        /* modelBuilder.Entity<RoleKey>().HasData(
             new RoleKey { Id = 1, Role = "Medico", HashedKey = medicoKey },
             new RoleKey { Id = 2, Role = "Secretaria", HashedKey = secretariaKey });
         OnModelCreatingPartial(modelBuilder);
         */
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
