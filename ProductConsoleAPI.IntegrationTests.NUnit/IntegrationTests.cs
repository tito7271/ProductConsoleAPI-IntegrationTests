using Microsoft.EntityFrameworkCore;
using ProductConsoleAPI.Business;
using ProductConsoleAPI.Business.Contracts;
using ProductConsoleAPI.Data.Models;
using ProductConsoleAPI.DataAccess;
using System;
using System.ComponentModel.DataAnnotations;

namespace ProductConsoleAPI.IntegrationTests.NUnit
{
    public class IntegrationTests
    {
        private TestProductsDbContext dbContext;
        private IProductsManager productsManager;

        [SetUp]
        public void SetUp()
        {
            this.dbContext = new TestProductsDbContext();
            this.productsManager = new ProductsManager(new ProductsRepository(this.dbContext));
        }


        [TearDown]
        public void TearDown()
        {
            this.dbContext.Database.EnsureDeleted();
            this.dbContext.Dispose();
        }


        //Positive test
        [Test]
        public async Task AddProductAsync_ShouldAddNewProduct()
        {
            // Arrange
            var newProduct = new Product()
            {
                OriginCountry = "Bulgaria",
                ProductName = "TestProduct",
                ProductCode = "AB12C",
                Price = 1.25m,
                Quantity = 100,
                Description = "Anything for description"
            };

            // Act
            await productsManager.AddAsync(newProduct);

            // Assert
            var dbProduct = await this.dbContext.Products.FirstOrDefaultAsync(p => p.ProductCode == newProduct.ProductCode);

            Assert.NotNull(dbProduct);
            Assert.That(dbProduct.OriginCountry, Is.EqualTo(newProduct.OriginCountry));
            Assert.That(dbProduct.ProductName, Is.EqualTo(newProduct.ProductName));
            Assert.That(dbProduct.ProductCode, Is.EqualTo(newProduct.ProductCode));
            Assert.That(dbProduct.Price, Is.EqualTo(newProduct.Price));
            Assert.That(dbProduct.Quantity, Is.EqualTo(newProduct.Quantity));
            Assert.That(dbProduct.Description, Is.EqualTo(newProduct.Description));
        }

        //Negative test
        [Test]
        public async Task AddProductAsync_TryToAddProductWithInvalidCredentials_ShouldThrowException()
        {
            // Arrange
            var newProduct = new Product()
            {
                OriginCountry = "Bulgaria",
                ProductName = "TestProduct",
                ProductCode = "AB12C",
                Price = -1m,
                Quantity = 100,
                Description = "Anything for description"
            };

            // Act & Assert
            var ex = Assert.ThrowsAsync<ValidationException>(async () => await productsManager.AddAsync(newProduct));
            var actual = await dbContext.Products.FirstOrDefaultAsync(p => p.ProductCode == newProduct.ProductCode);

            Assert.IsNull(actual);
            Assert.That(ex?.Message, Is.EqualTo("Invalid product!"));

        }

        [Test]
        public async Task DeleteProductAsync_WithValidProductCode_ShouldRemoveProductFromDb()
        {
            // Arrange
            var newProduct = new Product()
            {
                OriginCountry = "Bulgaria",
                ProductName = "TestProduct",
                ProductCode = "AB12C",
                Price = 1.25m,
                Quantity = 100,
                Description = "Anything for description"
            };

            await productsManager.AddAsync(newProduct);

            // Act
            await productsManager.DeleteAsync(newProduct.ProductCode);

            // Assert
            var dbProduct = await dbContext.Products.FirstOrDefaultAsync(p => p.ProductCode == newProduct.ProductCode);

            Assert.IsNull(dbProduct);
        }

        [TestCase(null)]
        [TestCase("   ")]
        public async Task DeleteProductAsync_TryToDeleteWithNullOrWhiteSpaceProductCode_ShouldThrowException(string invalidProductCode)
        {
            // Act & Assert
            var exception = Assert.ThrowsAsync<ArgumentException>(async () => await productsManager.DeleteAsync(invalidProductCode));
            Assert.That(exception.Message, Is.EqualTo("Product code cannot be empty."));
        }

        [Test]
        public async Task GetAllAsync_WhenProductsExist_ShouldReturnAllProducts()
        {
            // Arrange
            var newProduct = new Product()
            {
                OriginCountry = "Bulgaria",
                ProductName = "TestProduct",
                ProductCode = "AB12C",
                Price = 1.25m,
                Quantity = 100,
                Description = "Anything for description"
            };

            var newSecondProduct = new Product()
            {
                OriginCountry = "Bulgaria",
                ProductName = "SecondTestProduct",
                ProductCode = "AB123C1",
                Price = 3.25m,
                Quantity = 101,
                Description = "Anything for description"
            };

            await productsManager.AddAsync(newProduct);
            await productsManager.AddAsync(newSecondProduct);

            // Act
            var result = await productsManager.GetAllAsync();

            // Assert
            var firstProductResult = result.FirstOrDefault(x => x.ProductCode == newProduct.ProductCode);
            var secondProductResult = result.FirstOrDefault(x => x.ProductCode == newSecondProduct.ProductCode);

            Assert.That(result.Count(), Is.EqualTo(2));
            Assert.NotNull(result);
            Assert.IsNotEmpty(result);
            Assert.NotNull(firstProductResult);
            Assert.NotNull(secondProductResult);

            Assert.That(firstProductResult.OriginCountry, Is.EqualTo(newProduct.OriginCountry));
            Assert.That(firstProductResult.ProductName, Is.EqualTo(newProduct.ProductName));
            Assert.That(firstProductResult.ProductCode, Is.EqualTo(newProduct.ProductCode));
            Assert.That(firstProductResult.Price, Is.EqualTo(newProduct.Price));
            Assert.That(firstProductResult.Quantity, Is.EqualTo(newProduct.Quantity));
            Assert.That(firstProductResult.Description, Is.EqualTo(newProduct.Description));

            Assert.That(secondProductResult.OriginCountry, Is.EqualTo(newSecondProduct.OriginCountry));
            Assert.That(secondProductResult.ProductName, Is.EqualTo(newSecondProduct.ProductName));
            Assert.That(secondProductResult.ProductCode, Is.EqualTo(newSecondProduct.ProductCode));
            Assert.That(secondProductResult.Price, Is.EqualTo(newSecondProduct.Price));
            Assert.That(secondProductResult.Quantity, Is.EqualTo(newSecondProduct.Quantity));
            Assert.That(secondProductResult.Description, Is.EqualTo(newSecondProduct.Description));
        }

        [Test]
        public async Task GetAllAsync_WhenNoProductsExist_ShouldThrowKeyNotFoundException()
        {
            // Act & Assert
            var exception = Assert.ThrowsAsync<KeyNotFoundException>(async () => await productsManager.GetAllAsync());
            Assert.That(exception.Message, Is.EqualTo("No product found."));
        }

        [Test]
        public async Task SearchByOriginCountry_WithExistingOriginCountry_ShouldReturnMatchingProducts()
        {
            // Arrange
            var newProduct = new Product()
            {
                OriginCountry = "Bulgaria",
                ProductName = "TestProduct",
                ProductCode = "AB12C",
                Price = 1.25m,
                Quantity = 100,
                Description = "Anything for description"
            };

            var newSecondProduct = new Product()
            {
                OriginCountry = "Bulgaria",
                ProductName = "SecondTestProduct",
                ProductCode = "AB123C1",
                Price = 3.25m,
                Quantity = 101,
                Description = "Anything for description"
            };

            await productsManager.AddAsync(newProduct);
            await productsManager.AddAsync(newSecondProduct);

            // Act
            var result = await productsManager.SearchByOriginCountry(newProduct.OriginCountry);

            // Assert
            var firstProductResult = result.FirstOrDefault(x => x.ProductCode == newProduct.ProductCode);
            var secondProductResult = result.FirstOrDefault(x => x.ProductCode == newSecondProduct.ProductCode);

            Assert.That(result.Count, Is.EqualTo(2));
            Assert.NotNull(result);
            Assert.IsNotEmpty(result);
            Assert.NotNull(firstProductResult);
            Assert.NotNull(secondProductResult);

            Assert.That(firstProductResult.OriginCountry, Is.EqualTo(newProduct.OriginCountry));
            Assert.That(firstProductResult.ProductName, Is.EqualTo(newProduct.ProductName));
            Assert.That(firstProductResult.ProductCode, Is.EqualTo(newProduct.ProductCode));
            Assert.That(firstProductResult.Price, Is.EqualTo(newProduct.Price));
            Assert.That(firstProductResult.Quantity, Is.EqualTo(newProduct.Quantity));
            Assert.That(firstProductResult.Description, Is.EqualTo(newProduct.Description));

            Assert.That(secondProductResult.OriginCountry, Is.EqualTo(newSecondProduct.OriginCountry));
            Assert.That(secondProductResult.ProductName, Is.EqualTo(newSecondProduct.ProductName));
            Assert.That(secondProductResult.ProductCode, Is.EqualTo(newSecondProduct.ProductCode));
            Assert.That(secondProductResult.Price, Is.EqualTo(newSecondProduct.Price));
            Assert.That(secondProductResult.Quantity, Is.EqualTo(newSecondProduct.Quantity));
            Assert.That(secondProductResult.Description, Is.EqualTo(newSecondProduct.Description));
        }

        [Test]
        public async Task SearchByOriginCountryAsync_WithNonExistingOriginCountry_ShouldThrowKeyNotFoundException()
        {
            // Act & Assert
            var exception = Assert.ThrowsAsync<KeyNotFoundException>(async () => await productsManager.SearchByOriginCountry("Non existing Origin Name"));
            Assert.That(exception.Message, Is.EqualTo("No product found with the given first name."));
        }

        [Test]
        public async Task GetSpecificAsync_WithValidProductCode_ShouldReturnProduct()
        {
            // Arrange
            var newProduct = new Product()
            {
                OriginCountry = "Bulgaria",
                ProductName = "TestProduct",
                ProductCode = "AB12C",
                Price = 1.25m,
                Quantity = 100,
                Description = "Anything for description"
            };

            var newSecondProduct = new Product()
            {
                OriginCountry = "Bulgaria",
                ProductName = "SecondTestProduct",
                ProductCode = "AB123C1",
                Price = 3.25m,
                Quantity = 101,
                Description = "Anything for description"
            };

            await productsManager.AddAsync(newProduct);
            await productsManager.AddAsync(newSecondProduct);

            // Act
            var result = await productsManager.GetSpecificAsync(newProduct.ProductCode);

            // Assert
            Assert.NotNull(result);

            Assert.That(result.OriginCountry, Is.EqualTo(newProduct.OriginCountry));
            Assert.That(result.ProductName, Is.EqualTo(newProduct.ProductName));
            Assert.That(result.ProductCode, Is.EqualTo(newProduct.ProductCode));
            Assert.That(result.Price, Is.EqualTo(newProduct.Price));
            Assert.That(result.Quantity, Is.EqualTo(newProduct.Quantity));
            Assert.That(result.Description, Is.EqualTo(newProduct.Description));
        }

        [Test]
        public async Task GetSpecificAsync_WithInvalidProductCode_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            const string invalidProductCode = "Invalid Product Code";

            // Act & Assert
            var exception = Assert.ThrowsAsync<KeyNotFoundException>(async () => await productsManager.GetSpecificAsync(invalidProductCode));
            Assert.That(exception.Message, Is.EqualTo($"No product found with product code: {invalidProductCode}"));
        }

        [Test]
        public async Task UpdateAsync_WithValidProduct_ShouldUpdateProduct()
        {
            // Arrange
            var newProduct = new Product()
            {
                OriginCountry = "Bulgaria",
                ProductName = "TestProduct",
                ProductCode = "AB12C",
                Price = 1.25m,
                Quantity = 100,
                Description = "Anything for description"
            };

            await productsManager.AddAsync(newProduct);

            newProduct.ProductName = "UPDATED";

            // Act
            await productsManager.UpdateAsync(newProduct);

            // Assert
            var dbProduct = await dbContext.Products.FirstOrDefaultAsync(p => p.ProductCode == newProduct.ProductCode);

            Assert.NotNull(dbProduct);
            Assert.That(dbProduct.OriginCountry, Is.EqualTo(newProduct.OriginCountry));
            Assert.That(dbProduct.ProductName, Is.EqualTo(newProduct.ProductName));
            Assert.That(dbProduct.ProductCode, Is.EqualTo(newProduct.ProductCode));
            Assert.That(dbProduct.Price, Is.EqualTo(newProduct.Price));
            Assert.That(dbProduct.Quantity, Is.EqualTo(newProduct.Quantity));
            Assert.That(dbProduct.Description, Is.EqualTo(newProduct.Description));
        }

        [Test]
        public async Task UpdateAsync_WithInvalidProduct_ShouldThrowValidationException()
        {
            // Arrange
            var invalidNewProduct = new Product()
            {
                OriginCountry = "Bulgaria",
                ProductName = "TestProduct",
                ProductCode = "AB12C",
                Price = -1.25m,
                Quantity = 100,
                Description = "Anything for description"
            };

            // Act & Assert
            var exception = Assert.ThrowsAsync<ValidationException>(async () => await productsManager.UpdateAsync(invalidNewProduct));
            Assert.That(exception.Message, Is.EqualTo("Invalid prduct!"));
        }
    }
}
