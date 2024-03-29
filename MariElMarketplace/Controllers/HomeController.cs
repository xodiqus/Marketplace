﻿using Microsoft.Extensions.Logging;
using MariElMarketplace.Contexts;
using MariElMarketplace.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using MariElMarketplace.Calculators;
using MariElMarketplace.Models.ViewModels;
using MariElMarketplace.Helpers;
using System.Security.Claims;
using System.Collections.Generic;
using AutoMapper;

namespace MariElMarketplace.Controllers
{

    public class HomeController : Controller
    {
        private readonly CalculatorService _calculatorService;
        private readonly ILogger<HomeController> _logger;
        private readonly IMapper _mapper;
        private readonly Context _database;

        public HomeController(ILogger<HomeController> logger, Context context,
            CalculatorService calculatorService, IMapper mapper)
        {
            _database = context;
            _mapper = mapper;
            _logger = logger;
            _calculatorService = calculatorService;
        }

        public IActionResult СommodityProducerLk()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var role = _database.Roles.FirstOrDefault(x => x.UserId == userId);
            if (role == null || role.Role != Role.СommodityProducer)
            {
                return NotFound();
            }

            var products = _calculatorService.GetProductByFermerId(userId);

            return View("~/Views/СommodityProducer/Index.cshtml", products);
        }

        public IActionResult CustomerLk()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return View();
        }

        public IActionResult AdminLk()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var model = new AdminLkViewModel
            {
                AllProducts = _database.Products.ToList(),
                AllRequests = _database.Requests.ToList()
            };
            return View(model);
        }

        public IActionResult СarrierLk()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var myRequestsIds = _database.Requests.Where(x => x.CarrierId == userId).Select(x => x.ProductId).ToList();
            var otherRequestsId = _database.Requests.Where(x => !myRequestsIds.Contains(x.Id) && x.IsActive == true).Select(x => x.ProductId).ToList();

            var myProducts = new Dictionary<ProductWithCarryPrice, Requests>();
            foreach (var id in myRequestsIds)
            {
                var product = _database.Products.FirstOrDefault(x => x.Id == id);
                var reqvest = _database.Requests.FirstOrDefault(x => x.ProductId == id);
                var resMyProduct = _mapper.Map<ProductWithCarryPrice>(product);

                var distance = _database.Distances.FirstOrDefault(x => x.From == product.PlaceName && x.To == reqvest.ToPlaceName);
                resMyProduct.CarryPrice = distance.Km;
                myProducts.Add(resMyProduct, reqvest);
            }

            var otherProducts = new Dictionary<ProductWithCarryPrice, Requests>();
            foreach (var id in otherRequestsId)
            {
                var product = _database.Products.FirstOrDefault(x => x.Id == id);
                var reqvest = _database.Requests.FirstOrDefault(x => x.ProductId == id);
                var otherMyProduct = _mapper.Map<ProductWithCarryPrice>(product);

                var distance = _database.Distances.FirstOrDefault(x => x.From == product.PlaceName && x.To == reqvest.ToPlaceName);
                otherMyProduct.CarryPrice = distance.Km;
                otherProducts.Add(otherMyProduct, reqvest);
            }

            var model = new CarrierLkViewModel
            {
                MyProducts = myProducts,
                OtherProducts = otherProducts
            };
            return View(model);
        }

        public IActionResult Index()
        {
            var products = _database.Products.ToList();
            var bestPr = _calculatorService.GetBestProductTypes(products);
            var model = new HomeViewModel
            {
                BestProductTypes = bestPr,
                Products = products
            };
            return View(model);
        }

        public IActionResult Detail(int id)
        {
            var model = _calculatorService.GetBestSuggestions(id);
            return View(model);
        }

        public IActionResult MainCategories(ProductTypeEnum category)
        {
            var models = _calculatorService.GetByProductType(category);
            return View(models);
        }

        public IActionResult Categories(string subType)
        {
            var models = _calculatorService.GetProductBySubType(subType);
            return View(models);
        }

        public IActionResult CarrierNo(int requestId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var request = _database.Requests.FirstOrDefault(x => x.Id == requestId);
            request.IsActive = true;
            request.CarrierId = null;
            _database.SaveChanges();
            return Redirect("/Home/СarrierLk");
        }

        public IActionResult CarrierYes(int requestId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var request = _database.Requests.FirstOrDefault(x => x.Id == requestId);
            request.IsActive = false;
            request.CarrierId = userId;
            _database.SaveChanges();
            return Redirect("/Home/СarrierLk");
        }

        public IActionResult Buy(int id, string toRegion)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var product = _calculatorService.GetProductById(id);
            _database.Requests.Add(new Requests
            {
                UserId = userId,
                ProductId = product.Id,
                ToPlaceName = toRegion,
                FromPlaceName = product.PlaceName,
                IsActive = true
            });
            _database.SaveChanges();
            return Content("Success!");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
