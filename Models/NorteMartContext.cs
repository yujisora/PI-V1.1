using System.Data.Entity;

namespace PIV11.Models
{

    /// <summary>
    /// The single Entity Framework "gateway" to the NorteMart database. 
    /// Every controller that needs data creates one of these (inside a using block) and queries/saves through it.
    /// The database initializer is set to null in <c>Global.asax.cs</c> (Application_Start) to prevent EF from automatically 
    /// creating, deleting, or migrating the database; it only reads/writes the tables that were manually created.
    /// </summary>
    public class NorteMartContext : DbContext
    {
        /// <summary>
        /// NorteMartContext constructor that initializes the DbContext with the connection string named "NorteMartContext" from Web.config.
        /// This name must match the <connectionStrings> entry in Web.config (<seealso cref="Web.config.SNIPPET.txt"/>).
        /// </summary>
        public NorteMartContext() : base("name=NorteMartContext")
        {
        }
        /// <summary>
        /// A list of all products in the database. Each product has a unique UPC and may have associated 
        /// Foodstuff, NutritionData, IngredientsAllergens, and HealthAlert records.
        /// This can be pulled from an inventory Datbase from the store.
        /// </summary>
        public DbSet<Product> Products { get; set; }
        /// <summary>
        /// A list of attibutes only for foodstuffs, such as whether it is a beverage, meat, or produce. 
        /// Each Foodstuff record is linked to a Product by UPC.
        /// These are values that are not given by the main inventory database, but needed for the app.
        /// </summary>
        public DbSet<Foodstuff> Foodstuffs { get; set; }
        /// <summary>
        /// A list of nutrition data records, each linked to a Product by UPC.
        /// </summary>
        public DbSet<NutritionData> NutritionRecords { get; set; }
        /// <summary>
        /// A list of ingredients and allergens records, each linked to a Product by UPC.
        /// </summary>
        public DbSet<IngredientsAllergen> IngredientsAllergens { get; set; }
        /// <summary>
        /// A list of health alert records (warning seals), each linked to a Product by UPC.
        /// </summary>
        public DbSet<HealthAlert> HealthAlerts { get; set; }
        /// <summary>
        /// A list of people records.
        /// </summary
        public DbSet<Person> People { get; set; }
        /// <summary>
        /// The list of user accounts. Each user has a unique username and password, and may have associated People records.
        /// </summary>
        public DbSet<User> Users { get; set; }
        /// <summary>
        /// The history of edits made to products, including the username of the editor, the timestamp, and a description of the change.
        /// </summary>
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

            // Foodstuffs / NutritionData / IngredientsAllergens / HealthAlert all share their UPC as BOTH primary key and foreign key back
            // to Products (a "shared primary key" one-to-one relationship). Marking them optional on the Product side means a product can
            // exist even if one of its child rows hasn't been created yet
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
