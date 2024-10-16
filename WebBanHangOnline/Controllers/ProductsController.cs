﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebBanHangOnline.Models;

namespace WebBanHangOnline.Controllers
{
    public class ProductsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        // GET: Products
        public ActionResult Index()
        {
            var items = db.Products.ToList();
            
            return View(items);
        }

        public ActionResult Detail(string alias, int id)
        {
            // Retrieve the product from the database
            var item = db.Products.Find(id);
            if (item != null)
            {
                // Increment the view count
                item.ViewCount++;
                db.Entry(item).State = EntityState.Modified; // Mark the item as modified
                db.SaveChanges();

                // Get the count of reviews for the product
                var countReview = db.Reviews.Count(x => x.ProductId == id);
                ViewBag.CountReview = countReview;
            }
            else
            {
                // Handle case when product is not found (optional)
                return HttpNotFound();
            }

            return View(item);
        }

        public ActionResult ProductCategory(string alias,int id)
        {
            var items = db.Products.ToList();
            if (id > 0)
            {
                items = items.Where(x => x.ProductCategoryId == id).ToList();
            }
            var cate = db.ProductCategories.Find(id);
            if (cate != null)
            {
                ViewBag.CateName = cate.Title;
            }

            ViewBag.CateId = id;
            return View(items);
        }

        public ActionResult Partial_ItemsByCateId()
        {
            var items = db.Products.Where(x => x.IsHome && x.IsActive).Take(12).ToList();
            return PartialView(items);
        }

        public ActionResult Partial_ProductSales()
        {
            var items = db.Products.Where(x => x.IsSale && x.IsActive).Take(12).ToList();
            return PartialView(items);
        }

      
    }
}