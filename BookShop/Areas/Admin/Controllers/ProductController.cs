using BookShop.DataAccess.IRepository;
using BookShop.Models.ViewModels;
using BookShop.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using BookShop.Utility;
using Microsoft.Data.SqlClient;

namespace BookShopWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }
        public IActionResult Index()
        {
            List<Product> objProductList = _unitOfWork.Product.GetAll(includeProperties: "Category").ToList();

            return View(objProductList);
        }
    

        public IActionResult Upsert(int? id)
        {
            ProductVM productVM = new()
            {
                CategoryList = _unitOfWork.Category.GetAll().Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                }),
                Product = new Product()
            };
            if (id == null || id == 0)
            {
                //create
                return View(productVM);
            }
            else
            {
                //update
                productVM.Product = _unitOfWork.Product.Get(u => u.Id == id);
                return View(productVM);
            }

        }

        [HttpPost]
        public IActionResult Upsert(ProductVM productVM, IFormFile? file)//параметр "file" типа IFormFile (загружаемый файл).
        {
            if (ModelState.IsValid)
            {

                string wwwRootPath = _webHostEnvironment.WebRootPath;//Получает путь к корневой папке веб-приложения с помощью "_webHostEnvironment.WebRootPath".
                if (file != null)
                {
                    //Генерирует уникальное имя файла с помощью Guid.NewGuid().ToString() и Path.GetExtension(file.FileName) для получения расширения файла.
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    //Guid.NewGuid().ToString() - это вызов метода NewGuid() из класса Guid в языке программирования C#.
                    //Он генерирует уникальный идентификатор типа GUID (глобально уникальный идентификатор) и возвращает его в виде строки.
                    //Path.GetExtension(file.FileName) - в данном случае возвращает ".jpg" (тоесть расширение файла)

                    //Создает путь к папке для сохранения продукта с использованием Path.Combine(wwwRootPath, @"images\product").
                    string productPath = Path.Combine(wwwRootPath, @"images\product");

                    //Если у продукта уже был задан URL изображения (productVM.Product.ImageUrl не пустой), удаляет старое изображение,
                    //связанное с продуктом, удаляя файл по пути, указанному в productVM.Product.ImageUrl.
                    if (!string.IsNullOrEmpty(productVM.Product.ImageUrl))
                    {
                        //delete the old image
                        var oldImagePath =
                            Path.Combine(wwwRootPath, productVM.Product.ImageUrl.TrimStart('\\'));

                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }




                    //Копирует загруженный файл в папку продукта, используя FileStream и FileMode.Create.
                    using (var fileStream = new FileStream(Path.Combine(productPath, fileName), FileMode.Create))
                    {
                        file.CopyTo(fileStream);
                    }

                    //Задает новый URL изображения продукта, используя @"\images\product" + fileName.
                    productVM.Product.ImageUrl = @"\images\product\" + fileName;
                }
                
                    
                    //Если идентификатор продукта (productVM.Product.Id) равен 0, то есть продукт является новым,
                    //выполняет добавление продукта в репозиторий с помощью _unitOfWork.Product.Add(productVM.Product).
                    if (productVM.Product.Id == 0)
                    {
                        _unitOfWork.Product.Add(productVM.Product);
                    }
                    //В противном случае, если идентификатор продукта не равен 0, то есть продукт уже существует,
                    //выполняет обновление продукта в репозитории с помощью _unitOfWork.Product.Update(productVM.Product).
                    else
                    {
                        _unitOfWork.Product.Update(productVM.Product);
                    }

                    _unitOfWork.Save();
                    TempData["success"] = "Product created successfully";
                    return RedirectToAction("Index");
                
            }
            else
            {
                productVM.CategoryList = _unitOfWork.Category.GetAll().Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                });
                return View(productVM);
            }
        }

        //регионы и названия типа API CALLS предназначены только для лучшей читабельности, тоесть убрав их в логике ничего не поменяется и программа будет работать как и раньше
        #region API CALLS

        [HttpGet]
        public IActionResult GetAll()
        {
            List<Product> objProductList = _unitOfWork.Product.GetAll(includeProperties: "Category").ToList();
            return Json(new { data = objProductList });
        }


        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var productToBeDeleted = _unitOfWork.Product.Get(u => u.Id == id);
            if (productToBeDeleted == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }
            if (productToBeDeleted.ImageUrl != null)
            {
                var oldImagePath =
                               Path.Combine(_webHostEnvironment.WebRootPath,
                               productToBeDeleted.ImageUrl.TrimStart('\\'));


                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }
            }

            _unitOfWork.Product.Remove(productToBeDeleted);
            _unitOfWork.Save();

            return Json(new { success = true, message = "Delete Successful" });
        }
        #endregion
    }
}

        


