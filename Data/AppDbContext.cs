using System;
using System.Collections.Generic;
using MedCenter.Models;
using Microsoft.EntityFrameworkCore;

namespace MedCenter.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
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

            entity.HasOne(d => d.turno).WithMany(p => p.entradasClinicas)
                .HasForeignKey(d => d.turno_id)
                .HasConstraintName("entradasclinicas_turno_id_fkey");
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

            entity.HasOne(d => d.idNavigation).WithOne(p => p.medicos)
                .HasForeignKey<Medico>(d => d.id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("medicos_id_fkey");
        });

        modelBuilder.Entity<Paciente>(entity =>
        {
            entity.HasKey(e => e.id).HasName("pacientes_pkey");

            entity.Property(e => e.id).ValueGeneratedNever();
            entity.Property(e => e.dni).HasMaxLength(20);
            entity.Property(e => e.telefono).HasMaxLength(30);

            entity.HasOne(d => d.idNavigation).WithOne(p => p.pacientes)
                .HasForeignKey<Paciente>(d => d.id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("pacientes_id_fkey");
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

            entity.HasOne(d => d.idNavigation).WithOne(p => p.secretarias)
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
                .HasConstraintName("turnos_paciente_id_fkey");

            entity.HasOne(d => d.secretaria).WithMany(p => p.turnos)
                .HasForeignKey(d => d.secretaria_id)
                .HasConstraintName("turnos_secretaria_id_fkey");

            entity.HasOne(d => d.slot).WithMany(p => p.turnos)
                .HasForeignKey(d => d.slot_id)
                .HasConstraintName("turnos_slot_id_fkey");
        });

        modelBuilder.Entity<MedicoEspecialidad>()
            .HasKey(me => new { me.medicoId, me.especialidadId });

        modelBuilder.Entity<MedicoEspecialidad>()
            .HasOne(me => me.medico)
            .WithMany(m => m.medicoEspecialidades)
            .HasForeignKey(me => me.medicoId);

        modelBuilder.Entity<MedicoEspecialidad>()
            .HasOne(me => me.especialidad)
            .WithMany(e => e.medicoEspecialidades)
            .HasForeignKey(me => me.especialidadId);


        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
