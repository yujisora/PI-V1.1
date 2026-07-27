using System.Data.Entity;

namespace PIV11.Models
{
    /* =====================================================================
       NorteMartContext
       The single Entity Framework "gateway" to the NorteMart database.
       Every controller that needs data creates one of these (inside a
       using block) and queries/saves through it.

       IMPORTANT: Database.SetInitializer(null) is called in
       Global.asax.cs (Application_Start) so that EF NEVER tries to
       create, delete, or migrate the database automatically - it only
       reads/writes the tables you already built by hand.
       ===================================================================== */
    public class NorteMartContext : DbContext
    {
        // "NorteMartContext" here must match the <connectionStrings> name
        // added to Web.config (see Web.config.SNIPPET.txt).
        public NorteMartContext() : base("name=NorteMartContext")
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Foodstuff> Foodstuffs { get; set; }
        public DbSet<NutritionData> NutritionRecords { get; set; }
        public DbSet<IngredientsAllergen> IngredientsAllergens { get; set; }
        public DbSet<HealthAlert> HealthAlerts { get; set; }
        public DbSet<Person> People { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<EditHistoryRecord> EditHistory { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // Turn off EF's default pluralized-table-name convention -
            // our table names already match exactly (Products, People, etc).
            modelBuilder.Conventions.Remove<System.Data.Entity.ModelConfiguration.Conventions.PluralizingTableNameConvention>();

            // Map each entity explicitly to its real table name.
            modelBuilder.Entity<Product>().ToTable("Products");
            modelBuilder.Entity<Foodstuff>().ToTable("Foodstuffs");
            modelBuilder.Entity<NutritionData>().ToTable("NutritionData");
            modelBuilder.Entity<IngredientsAllergen>().ToTable("IngredientsAllergens");
            modelBuilder.Entity<HealthAlert>().ToTable("HealthAlert");
            modelBuilder.Entity<Person>().ToTable("People");
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<EditHistoryRecord>().ToTable("EditHistory");

            // Foodstuffs / NutritionData / IngredientsAllergens / HealthAlert
            // all share their UPC as BOTH primary key and foreign key back
            // to Products (a "shared primary key" one-to-one relationship).
            // Marking them optional on the Product side means a product can
            // exist even if one of its child rows hasn't been created yet.
            modelBuilder.Entity<Product>()
                .HasOptional(p => p.Foodstuff)
                .WithRequired(f => f.Product);

            modelBuilder.Entity<Product>()
                .HasOptional(p => p.NutritionData)
                .WithRequired(n => n.Product);

            modelBuilder.Entity<Product>()
                .HasOptional(p => p.IngredientsAllergens)
                .WithRequired(i => i.Product);

            modelBuilder.Entity<Product>()
                .HasOptional(p => p.HealthAlert)
                .WithRequired(h => h.Product);
        }
    }
}
