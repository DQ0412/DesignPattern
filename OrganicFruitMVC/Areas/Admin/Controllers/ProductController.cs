using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using OrganicFoodMVC.DataAccess.Repository.IRepository;
using OrganicFoodMVC.Models;
using OrganicFoodMVC.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.IO;
using Microsoft.AspNetCore.Authorization;
using OrganicFoodMVC.Utility;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;
using HtmlAgilityPack;

namespace OrganicFoodMVC.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        // info path to save image
        private readonly IWebHostEnvironment _hostEnvironment;

        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment hostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _hostEnvironment = hostEnvironment;

        }

        public IActionResult Index()
        {
            return View();
        }

        // insert and update product
        public IActionResult Upsert(int? id)
        {
            IEnumerable<Category> CatList = _unitOfWork.Category.GetAll();
            ProductVM productVM = new ProductVM()
            {
                Product = new Product(),
                CategoryList = CatList.Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Id.ToString()
                }),
                BrandList = _unitOfWork.Brand.GetAll().Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Id.ToString()
                }),
                UnitList = _unitOfWork.Unit.GetAll().Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Id.ToString()
                })
            };
            if (id == null)
            {
                //this is for create
                return View(productVM);
            }
            //this is for edit
            productVM.Product = _unitOfWork.Product.Get(id.GetValueOrDefault());
            if (productVM.Product == null)
            {
                return NotFound();
            }
            return View(productVM);

        }

        // info path to save image
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(ProductVM productVM)
        {

            if (ModelState.IsValid)
            {
                string webRootPath = _hostEnvironment.WebRootPath;
                var files = HttpContext.Request.Form.Files;
                if (files.Count > 0)
                {
                    string fileName = Guid.NewGuid().ToString();
                    var uploads = Path.Combine(webRootPath, @"images\products");
                    var extenstion = Path.GetExtension(files[0].FileName);

                    if (productVM.Product.ImageUrl != null)
                    {
                        //this is an edit and we need to remove old image

                        var imagePath = Path.Combine(webRootPath, productVM.Product.ImageUrl.TrimStart('\\'));
                        if (System.IO.File.Exists(imagePath))
                        {
                            System.IO.File.Delete(imagePath);
                        }
                    }
                    using (var filesStreams = new FileStream(Path.Combine(uploads, fileName + extenstion), FileMode.Create))
                    {
                        files[0].CopyTo(filesStreams);
                    }
                    productVM.Product.ImageUrl = @"\images\products\" + fileName + extenstion;
                }
                else
                {
                    //update when they do not change the image
                    if (productVM.Product.Id != 0)
                    {
                        Product objFromDb = _unitOfWork.Product.Get(productVM.Product.Id);
                        productVM.Product.ImageUrl = objFromDb.ImageUrl;
                    }
                }

                // insert
                if (productVM.Product.Id == 0)
                {
                    _unitOfWork.Product.Add(productVM.Product);

                }
                else // update
                {
                    _unitOfWork.Product.Update(productVM.Product);
                }
                _unitOfWork.Save();
                return RedirectToAction(nameof(Index));
            }
            else // assign cateList and BrandList if modelState.IsVaild false
            {
                IEnumerable<Category> CatList = _unitOfWork.Category.GetAll();
                productVM.CategoryList = CatList.Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Id.ToString()
                });
                productVM.BrandList = _unitOfWork.Brand.GetAll().Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Id.ToString()
                });
                productVM.UnitList = _unitOfWork.Unit.GetAll().Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Id.ToString()
                });
                if (productVM.Product.Id != 0)
                {
                    productVM.Product = _unitOfWork.Product.Get(productVM.Product.Id);
                }
            }
            return View(productVM);
        }

        public ActionResult ExportToExcel()
        {
            var doctors = _unitOfWork.Product.GetAll(includeProperties: "Category,Brand,Unit");

            byte[] fileContents;
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            ExcelPackage Ep = new ExcelPackage();
            ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("DoctorsInfo");
            Sheet.Cells["B1"].Value = "Mã Sản Phẩm";
            Sheet.Cells["C1"].Value = "Tên Sản Phẩm";
            //Sheet.Cells["C1"].Value = "Mô tả ";
            Sheet.Cells["D1"].Value = "Số lượng";
            Sheet.Cells["E1"].Value = "Giá";
            Sheet.Cells["F1"].Value = "Link ảnh";
            Sheet.Cells["G1"].Value = "Loại sản phẩm";
            Sheet.Cells["H1"].Value = "Thương hiệu";
            Sheet.Cells["I1"].Value = "Đơn vị";

            //row bằng 2 vì hàng 1 là hàng tiêu đề
            int row = 2;
            foreach (var item in doctors)
            {
                //var doc = new HtmlDocument();
                //doc.LoadHtml(item.Discription);
                //var innertext = doc.DocumentNode.InnerText; 
                Sheet.Cells[string.Format("B{0}", row)].Value = item.Id;
                Sheet.Cells[string.Format("C{0}", row)].Value = item.Name;
                //Sheet.Cells[string.Format("C{0}", row)].Value = innertext;
                Sheet.Cells[string.Format("D{0}", row)].Value = item.Quantity;
                Sheet.Cells[string.Format("E{0}", row)].Value = item.Price;
                Sheet.Cells[string.Format("F{0}", row)].Value = item.ImageUrl;
                Sheet.Cells[string.Format("G{0}", row)].Value = item.Category.Name;
                Sheet.Cells[string.Format("H{0}", row)].Value = item.Brand.Name;
                Sheet.Cells[string.Format("I{0}", row)].Value = item.Unit.Name;

                row++;
            }


            Sheet.Cells["A:AZ"].AutoFitColumns();
            Response.Clear();
            Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            fileContents = Ep.GetAsByteArray();

            if (fileContents == null || fileContents.Length == 0)
            {
                return NotFound();
            }

            return File(
                fileContents: fileContents,
                contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileDownloadName: "SanPhamcuaOrganicFruit.xlsx"
            );
        }

        private ActionResult NotFound()
        {
            throw new NotImplementedException();
        }


        #region API CALLS

        // API get product
        [HttpGet]
        public IActionResult GetAll()
        {
            var allObj = _unitOfWork.Product.GetAll(includeProperties: "Category,Brand,Unit");
            return Json(new { data = allObj });
        }

        // API delete product
        [HttpDelete]
        public IActionResult Delete(int id)
        {
            var objFromDb = _unitOfWork.Product.Get(id);
            if(objFromDb == null)
            {
                return Json(new { success = false, message = "Xóa sản phẩm thất bại!" });
            }

            // delete image
            string webRootPath = _hostEnvironment.WebRootPath;
            var imagePath = Path.Combine(webRootPath, objFromDb.ImageUrl.TrimStart('\\'));
            if (System.IO.File.Exists(imagePath))
            {
                System.IO.File.Delete(imagePath);
            }

            _unitOfWork.Product.Remove(objFromDb);
            _unitOfWork.Save();
            return Json(new { success = true, message = "Xóa sản phẩm thành công!" });
        }

        #endregion
    }
}

/*<!--In the desolate village of Shadows Hollow, where the gray clouds perpetually shrouded the sky, lived two ill-fated souls named Evelyn and Edgar. Their lives were entwined by a curse that seemed to echo through the haunting whispers of the dense, fog-covered woods that surrounded their homes.

Evelyn, a frail but kind-hearted girl, and Edgar, a brooding and mysterious young man, were deeply in love. However, a dark secret tainted their union — a centuries-old curse that condemned every firstborn child in their families to a life of perpetual suffering.

Their families, blinded by generations of grief and despair, forbade the love between Evelyn and Edgar. The villagers believed that breaking the curse required the ultimate sacrifice — the death of one of the cursed lovers. Unbeknownst to the young couple, their union was seen as a desperate attempt to defy fate and break free from the relentless grip of the curse.

As the villagers fueled the flames of superstition, Evelyn and Edgar clung to their love, determined to rewrite their tragic destiny. They ventured into the forbidden woods, guided by ancient whispers that promised a solution to their affliction.

In the heart of the ominous forest, they discovered a dilapidated shrine where a grotesque figure, the embodiment of the curse itself, demanded a sacrifice. Terrified but resolute, Evelyn and Edgar faced an agonizing choice — to save their families from perpetual suffering, one of them must willingly embrace the cold embrace of death.

In an anguished farewell, Evelyn offered herself as the sacrifice, her tearful eyes locking with Edgar's in a silent exchange of love and despair. The curse, momentarily appeased, unleashed a torrential storm that mirrored the tempest within their hearts.

Edgar, left alone in the wake of the storm, carried the burden of the curse and the memory of his beloved Evelyn. Shadows Hollow, forever cloaked in sorrow, stood witness to a tragedy that transcended time, as the village's inhabitants whispered tales of the ill-fated lovers whose love was both their salvation and demise.

Evelyn's sacrifice became a somber legend, a chilling reminder that love, in the cruel tapestry of Shadows Hollow, could be as destructive as it was beautiful. The cursed village, forever haunted by the echoes of a love lost to the ages, stood as a desolate monument to the tragic consequences of defying the cruel whims of destiny.

Years passed, and the desolation that enshrouded Shadows Hollow intensified. The curse, though momentarily sated by Evelyn's sacrifice, continued to cast its ominous shadow over the village. The air was thick with grief, and the once-vibrant community dwindled into a ghostly existence.

Edgar, burdened by guilt and the weight of his lost love, became a solitary figure, wandering the twisted paths of the woods that had claimed Evelyn. Each step he took echoed with the memories of their stolen moments together and the heart-wrenching decision that tore them apart.

The villagers, gripped by fear and superstition, avoided Edgar, whispering that the very presence of the cursed lover could invoke the wrath of the malevolent force that had claimed Evelyn. Shadows Hollow became a place where time stood still, a purgatory for lost souls ensnared in the tendrils of an age-old curse.

One fateful night, as the moon cast an eerie glow over the village, an ethereal figure emerged from the misty woods. It was Evelyn, but not as the villagers remembered her. She appeared as a spectral being, a manifestation of the curse's relentless hold on her soul.

Driven by an otherworldly force, Evelyn's ghost sought out Edgar, her translucent form gliding through the darkened village. When she finally found him, their reunion was a bittersweet dance between the realms of the living and the dead. Edgar, tormented by grief and longing, was both captivated and horrified by the spectral presence of his lost love.

As the ghostly couple embraced beneath the skeletal branches of the cursed woods, a haunting lament echoed through Shadows Hollow. The curse, sensing the undying connection between Evelyn and Edgar, intensified its grip on their entwined souls. The villagers, paralyzed by fear, watched the tragic reunion unfold with a mixture of sorrow and dread.

In a final act of desperation, Edgar pleaded with the curse to release them from its malevolent grasp. The ghostly lovers, their figures intertwined like wisps of smoke, slowly dissipated into the night. The curse, momentarily appeased, loosened its grip on Shadows Hollow, leaving behind an emptiness that mirrored the hollowness in the hearts of those who remained.

The once-vibrant village, now a mere shell of its former self, became a cautionary tale whispered by the wind through the twisted trees. Shadows Hollow stood as a testament to the devastating consequences of a love that defied the cruel whims of destiny — a love that, even in death, refused to be extinguished.-->
*/
