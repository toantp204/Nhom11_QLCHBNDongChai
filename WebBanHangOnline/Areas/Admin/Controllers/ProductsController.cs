﻿using PagedList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebBanHangOnline.Models;
using WebBanHangOnline.Models.EF;

namespace WebBanHangOnline.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin,Employee")]
    public class ProductsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        // GET: Admin/Products
        public ActionResult Index(int? page)
        {
            // Fetch products and order by descending Id
            var items = db.Products.OrderByDescending(x => x.Id);

            // Define page size
            int pageSize = 10;

            // Set default page to 1 if null
            int pageIndex = page ?? 1;

            // Paginate items
            var pagedItems = items.ToPagedList(pageIndex, pageSize);

            // Pass data to the view via ViewBag
            ViewBag.PageSize = pageSize;
            ViewBag.Page = pageIndex;

            // Return the view with the paged items
            return View(pagedItems);
        }


        public ActionResult Add()
        {
            ViewBag.ProductCategory = new SelectList(db.ProductCategories.ToList(), "Id", "Title");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Add(Product model, List<string> Images, List<int> rDefault)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra giá khuyến mãi
                if (model.PriceSale.HasValue && model.PriceSale > model.Price)
                {
                    ModelState.AddModelError("PriceSale", "Giá khuyến mãi không được lớn hơn giá.");
                }

                // Kiểm tra trùng lặp tên sản phẩm
                var existingProduct = db.Products
                    .FirstOrDefault(p => p.Title == model.Title);

                if (existingProduct != null)
                {
                    ModelState.AddModelError("Title", "Tên sản phẩm đã tồn tại.");
                }

                if (ModelState.IsValid)
                {
                    if (Images != null && Images.Count > 0)
                    {
                        for (int i = 0; i < Images.Count; i++)
                        {
                            if (i + 1 == rDefault[0])
                            {
                                model.Image = Images[i];
                                model.ProductImage.Add(new ProductImage
                                {
                                    ProductId = model.Id,
                                    Image = Images[i],
                                    IsDefault = true
                                });
                            }
                            else
                            {
                                model.ProductImage.Add(new ProductImage
                                {
                                    ProductId = model.Id,
                                    Image = Images[i],
                                    IsDefault = false
                                });
                            }
                        }
                    }
                    model.CreatedDate = DateTime.Now;
                    model.ModifiedDate = DateTime.Now;
                    if (string.IsNullOrEmpty(model.SeoTitle))
                    {
                        model.SeoTitle = model.Title;
                    }
                    if (string.IsNullOrEmpty(model.Alias))
                        model.Alias = WebBanHangOnline.Models.Common.Filter.FilterChar(model.Title);
                    db.Products.Add(model);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }
            ViewBag.ProductCategory = new SelectList(db.ProductCategories.ToList(), "Id", "Title");
            return View(model);
        }



        public ActionResult Edit(int id)    
        {
            ViewBag.ProductCategory = new SelectList(db.ProductCategories.ToList(), "Id", "Title");
            var item = db.Products.Find(id);
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Product model)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra giá khuyến mãi
                if (model.PriceSale.HasValue && model.PriceSale > model.Price)
                {
                    ModelState.AddModelError("PriceSale", "Giá khuyến mãi không được lớn hơn giá.");
                }

                // Kiểm tra trùng lặp tên sản phẩm
                var existingProduct = db.Products
                    .FirstOrDefault(p => p.Title == model.Title && p.Id != model.Id);

                if (existingProduct != null)
                {
                    ModelState.AddModelError("Title", "Tên sản phẩm đã tồn tại.");
                }

                if (ModelState.IsValid)
                {
                    model.ModifiedDate = DateTime.Now;
                    model.Alias = WebBanHangOnline.Models.Common.Filter.FilterChar(model.Title);
                    db.Products.Attach(model);
                    db.Entry(model).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }
            ViewBag.ProductCategory = new SelectList(db.ProductCategories.ToList(), "Id", "Title");
            return View(model);
        }


        [HttpPost]
        public ActionResult Delete(int id)
        {
            var item = db.Products.Find(id);
            if (item != null)
            {
                var checkImg = item.ProductImage.Where(x => x.ProductId == item.Id);
                if (checkImg != null)
                {
                    foreach(var img in checkImg)
                    {
                        db.ProductImages.Remove(img);
                        db.SaveChanges();
                    }
                }
                db.Products.Remove(item);
                db.SaveChanges();
                return Json(new { success = true });
            }

            return Json(new { success = false });
        }

        [HttpPost]
        public ActionResult IsActive(int id)
        {
            var item = db.Products.Find(id);
            if (item != null)
            {
                item.IsActive = !item.IsActive;
                db.Entry(item).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
                return Json(new { success = true, isAcive = item.IsActive });
            }

            return Json(new { success = false });
        }
        [HttpPost]
        public ActionResult IsHome(int id)
        {
            var item = db.Products.Find(id);
            if (item != null)
            {
                item.IsHome = !item.IsHome;
                db.Entry(item).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
                return Json(new { success = true, IsHome = item.IsHome });
            }

            return Json(new { success = false });
        }

        [HttpPost]
        public ActionResult IsSale(int id)
        {
            var item = db.Products.Find(id);
            if (item != null)
            {
                item.IsSale = !item.IsSale;
                db.Entry(item).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
                return Json(new { success = true, IsSale = item.IsSale });
            }

            return Json(new { success = false });
        }
    }
}