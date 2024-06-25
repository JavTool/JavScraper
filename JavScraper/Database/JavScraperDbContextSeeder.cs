using JavScraper.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace JavScraper.Database
{
    public class JavScraperDbContextSeeder
    {
        public static void Seed(/**JavScraperContext context**/)
        {

            //context.Database.EnsureDeleted();
            //context.Database.EnsureCreated();

            //context.Movies.Add(new Movie()
            //{
            //    Title = "Movie"
            //});
            //context.ProductCategories.Add(new ProductCategory()
            //{
            //    CategoryName = "novel"
            //});
            //context.ProductCategories.Add(new ProductCategory()
            //{
            //    CategoryName = "poetry",
            //    ParentCategory = context.ProductCategories.Local.Single(p => p.CategoryName == "novel")
            //});
            //context.ProductCategories.Add(new ProductCategory()
            //{
            //    CategoryName = "detective story"
            //});
            //context.ProductCategories.Add(new ProductCategory()
            //{
            //    CategoryName = "fantasy",
            //    ParentCategory = context.ProductCategories.Local.Single(p => p.CategoryName == "novel")
            //});
            //context.ProductCategories.Add(new ProductCategory()
            //{
            //    CategoryName = "pop art",
            //    ParentCategory = context.ProductCategories.Local.Single(p => p.CategoryName == "fantasy")
            //});
            //context.ProductCategories.Add(new ProductCategory()
            //{
            //    CategoryName = "textbook"
            //});
            //context.ProductCategories.Add(new ProductCategory()
            //{
            //    CategoryName = "research book",
            //    ParentCategory = context.ProductCategories.Local.Single(p => p.CategoryName == "textbook")
            //});
            //context.ProductCategories.Add(new ProductCategory()
            //{
            //    CategoryName = "poem",
            //    ParentCategory = context.ProductCategories.Local.Single(p => p.CategoryName == "novel")
            //});
            //context.ProductCategories.Add(new ProductCategory()
            //{
            //    CategoryName = "collection",
            //    ParentCategory = context.ProductCategories.Local.Single(p => p.CategoryName == "textbook")
            //});
            //context.ProductCategories.Add(new ProductCategory()
            //{
            //    CategoryName = "dictionary",
            //    ParentCategory = context.ProductCategories.Local.Single(p => p.CategoryName == "collection")
            //});

            //context.Products.Add(new Product()
            //{
            //    ProductName = "Shakespeare W. Shakespeare's dramatische Werke",
            //    Price = 78,
            //    Category = context.ProductCategories.Local.Single(p => p.CategoryName == "prose")
            //});
            //context.Products.Add(new Product()
            //{
            //    ProductName = "King Stephen. 'Salem's Lot",
            //    Price = 67,
            //    Category = context.ProductCategories.Local.Single(p => p.CategoryName == "poetry")
            //});
            //context.Products.Add(new Product()
            //{
            //    ProductName = "Plutarchus. Plutarch's moralia",
            //    Price = 89,
            //    Category = context.ProductCategories.Local.Single(p => p.CategoryName == "prose")
            //});
            //context.Products.Add(new Product()
            //{
            //    ProductName = "Twain Mark. Ventures of Huckleberry Finn",
            //    Price = 34,
            //    Category = context.ProductCategories.Local.Single(p => p.CategoryName == "novel")
            //});
            //context.Products.Add(new Product()
            //{
            //    ProductName = "Harrison G. B. England in Shakespeare's day",
            //    Price = 540,
            //    Category = context.ProductCategories.Local.Single(p => p.CategoryName == "novel")
            //});
            //context.Products.Add(new Product()
            //{
            //    ProductName = "Corkett Anne. The salamander's laughter",
            //    Price = 5,
            //    Category = context.ProductCategories.Local.Single(p => p.CategoryName == "poem")
            //});
            //context.Products.Add(new Product()
            //{
            //    ProductName = "Lightman Alan. Einstein''s dreams",
            //    Price = 5,
            //    Category = context.ProductCategories.Local.Single(p => p.CategoryName == "poem")
            //});

            //context.Companies.Add(new Company()
            //{
            //    CompanyName = "Borland UK CodeGear Division",
            //    Web = "support.codegear.com/"
            //});
            //context.Companies.Add(new Company()
            //{
            //    CompanyName = "Alfa-Bank",
            //    Web = "www.alfabank.com"
            //});
            //context.Companies.Add(new Company()
            //{
            //    CompanyName = "Pioneer Pole Buildings, Inc.",
            //    Web = "www.pioneerpolebuildings.com"
            //});
            //context.Companies.Add(new Company()
            //{
            //    CompanyName = "Orion Telecoms (Pty) Ltd.",
            //    Web = "www.oriontele.com"
            //});
            //context.Companies.Add(new Company()
            //{
            //    CompanyName = "Orderbase Consulting GmbH",
            //    Web = "orderbase.de"
            //});

            //context.Orders.Add(new Order()
            //{
            //    OrderDate = new DateTime(2007, 4, 11),
            //    Company = context.Companies.Local.Single(c => c.CompanyName == "Borland UK CodeGear Division")
            //});
            //context.Orders.Add(new Order()
            //{
            //    OrderDate = new DateTime(2006, 3, 11),
            //    Company = context.Companies.Local.Single(c => c.CompanyName == "Borland UK CodeGear Division")
            //});
            //context.Orders.Add(new Order()
            //{
            //    OrderDate = new DateTime(2006, 8, 6),
            //    Company = context.Companies.Local.Single(c => c.CompanyName == "Alfa-Bank")
            //});
            //context.Orders.Add(new Order()
            //{
            //    OrderDate = new DateTime(2004, 7, 6),
            //    Company = context.Companies.Local.Single(c => c.CompanyName == "Alfa-Bank")
            //});
            //context.Orders.Add(new Order()
            //{
            //    OrderDate = new DateTime(2006, 8, 8),
            //    Company = context.Companies.Local.Single(c => c.CompanyName == "Alfa-Bank")
            //});
            //context.Orders.Add(new Order()
            //{
            //    OrderDate = new DateTime(2003, 3, 1),
            //    Company = context.Companies.Local.Single(c => c.CompanyName == "Pioneer Pole Buildings, Inc.")
            //});
            //context.Orders.Add(new Order()
            //{
            //    OrderDate = new DateTime(2005, 8, 6),
            //    Company = context.Companies.Local.Single(c => c.CompanyName == "Orion Telecoms (Pty) Ltd.")
            //});
            //context.Orders.Add(new Order()
            //{
            //    OrderDate = new DateTime(2006, 8, 1),
            //    Company = context.Companies.Local.Single(c => c.CompanyName == "Orion Telecoms (Pty) Ltd.")
            //});
            //context.Orders.Add(new Order()
            //{
            //    OrderDate = new DateTime(2007, 7, 1),
            //    Company = context.Companies.Local.Single(c => c.CompanyName == "Orion Telecoms (Pty) Ltd.")
            //});
            //context.Orders.Add(new Order()
            //{
            //    OrderDate = new DateTime(2007, 2, 6),
            //    Company = context.Companies.Local.Single(c => c.CompanyName == "Orderbase Consulting GmbH")
            //});
            //context.Orders.Add(new Order()
            //{
            //    OrderDate = new DateTime(2007, 8, 1),
            //    Company = context.Companies.Local.Single(c => c.CompanyName == "Orderbase Consulting GmbH")
            //});

            //context.SaveChanges();
        }
    }
}
