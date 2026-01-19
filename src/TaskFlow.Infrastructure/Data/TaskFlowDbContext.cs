using Microsoft.EntityFrameworkCore;
using TaskFlow.Core.Entities;
using TaskFlow.Core.Enums;

namespace TaskFlow.Infrastructure.Data;

public class TaskFlowDbContext : DbContext
{
    public TaskFlowDbContext(DbContextOptions<TaskFlowDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<ProjectMember> ProjectMembers { get; set; }
    public DbSet<TaskItem> Tasks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure all DateTime properties to use UTC
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                {
                    property.SetColumnType("timestamp with time zone");
                }
            }
        }


        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedNever(); // We'll generate GUIDs in code

            entity.Property(e => e.Email)
                .HasColumnName("email")
                .HasMaxLength(255)
                .IsRequired();

            entity.HasIndex(e => e.Email)
                .IsUnique()
                .HasDatabaseName("ix_users_email");

            entity.Property(e => e.PasswordHash)
                .HasColumnName("password_hash")
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(e => e.FullName)
                .HasColumnName("full_name")
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();
        });

        // Project configuration
        modelBuilder.Entity<Project>(entity =>
        {
            entity.ToTable("projects");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            entity.Property(e => e.Name)
                .HasColumnName("name")
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(e => e.Description)
                .HasColumnName("description")
                .HasMaxLength(1000);

            entity.Property(e => e.OwnerId)
                .HasColumnName("owner_id")
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            // Relationships
            entity.HasOne(e => e.Owner)
                .WithMany()
                .HasForeignKey(e => e.OwnerId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_projects_owner_id");

            entity.HasIndex(e => e.OwnerId)
                .HasDatabaseName("ix_projects_owner_id");
        });

        // ProjectMember configuration
        modelBuilder.Entity<ProjectMember>(entity =>
        {
            entity.ToTable("project_members");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            entity.Property(e => e.ProjectId)
                .HasColumnName("project_id")
                .IsRequired();

            entity.Property(e => e.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            entity.Property(e => e.Role)
                .HasColumnName("role")
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.JoinedAt)
                .HasColumnName("joined_at")
                .IsRequired();

            // Relationships
            entity.HasOne(e => e.Project)
                .WithMany(p => p.Members)
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_project_members_project_id");

            entity.HasOne(e => e.User)
                .WithMany(u => u.ProjectMembers)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_project_members_user_id");

            // Unique constraint - user can only be member once per project
            entity.HasIndex(e => new { e.ProjectId, e.UserId })
                .IsUnique()
                .HasDatabaseName("ix_project_members_project_user");

            entity.HasIndex(e => e.ProjectId)
                .HasDatabaseName("ix_project_members_project_id");

            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("ix_project_members_user_id");
        });

        // TaskItem configuration
        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.ToTable("tasks");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            entity.Property(e => e.Title)
                .HasColumnName("title")
                .HasMaxLength(300)
                .IsRequired();

            entity.Property(e => e.Description)
                .HasColumnName("description")
                .HasMaxLength(2000);

            entity.Property(e => e.ProjectId)
                .HasColumnName("project_id")
                .IsRequired();

            entity.Property(e => e.AssignedToId)
                .HasColumnName("assigned_to_id");

            entity.Property(e => e.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.Priority)
                .HasColumnName("priority")
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            entity.Property(e => e.DueDate)
                .HasColumnName("due_date");

            // Relationships
            entity.HasOne(e => e.Project)
                .WithMany(p => p.Tasks)
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_tasks_project_id");

            entity.HasOne(e => e.AssignedTo)
                .WithMany()
                .HasForeignKey(e => e.AssignedToId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_tasks_assigned_to_id");

            // Indexes
            entity.HasIndex(e => e.ProjectId)
                .HasDatabaseName("ix_tasks_project_id");

            entity.HasIndex(e => e.AssignedToId)
                .HasDatabaseName("ix_tasks_assigned_to_id");

            entity.HasIndex(e => e.Status)
                .HasDatabaseName("ix_tasks_status");

            entity.HasIndex(e => e.CreatedAt)
                .HasDatabaseName("ix_tasks_created_at");
        });
    }
}