using Bulky.DataAccess.Repository;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Bulky.Models;
using Bulky.Utility;
using Stripe.Checkout;
using Stripe.Checkout;

namespace BulkyWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        [BindProperty]
        public ShoppingCartVM ShoppingCartVM { get; set; }

        public CartController(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public IActionResult Index()
        {
            var claimsIdentity = User.Identity as ClaimsIdentity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            ShoppingCartVM = new()
            {
                ShoppingCartList =
                    _unitOfWork.ShoppingCart.GetAll(x => x.ApplicationUserId == userId, includeProperties: "Product"),
                OrderHeader = new OrderHeader()
            };

            foreach (var cart in ShoppingCartVM.ShoppingCartList)
            {
                cart.Price = GetPriceBasedOnQuantity(cart);
                ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }
            
            return View(ShoppingCartVM);
        }

        public IActionResult Summary()
        {
            var claimsIdentity = User.Identity as ClaimsIdentity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            ShoppingCartVM = new()
            {
                ShoppingCartList =
                    _unitOfWork.ShoppingCart.GetAll(x => x.ApplicationUserId == userId, includeProperties: "Product"),
                OrderHeader = new OrderHeader()
                {
                    ApplicationUser = _unitOfWork.ApplicationUser.Get(x => x.Id == userId)
                }
            };

            ShoppingCartVM.OrderHeader.Name = ShoppingCartVM.OrderHeader.ApplicationUser.Name;
            ShoppingCartVM.OrderHeader.PhoneNumber = ShoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber;
            ShoppingCartVM.OrderHeader.StreetAddress = ShoppingCartVM.OrderHeader.ApplicationUser.StreetAddress;
            ShoppingCartVM.OrderHeader.City = ShoppingCartVM.OrderHeader.ApplicationUser.City;
            ShoppingCartVM.OrderHeader.State = ShoppingCartVM.OrderHeader.ApplicationUser.State;
            ShoppingCartVM.OrderHeader.PostalCode = ShoppingCartVM.OrderHeader.ApplicationUser.PostalCode;

            foreach (var cart in ShoppingCartVM.ShoppingCartList)
            {
                cart.Price = GetPriceBasedOnQuantity(cart);
                ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }

            return View(ShoppingCartVM);
        }

        [HttpPost]
        [ActionName("Summary")]
        public IActionResult SummaryPOST()
        {
	        var claimsIdentity = (ClaimsIdentity)User.Identity;
	        var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

	        ShoppingCartVM.ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId,
		        includeProperties: "Product");

	        ShoppingCartVM.OrderHeader.OrderDate = DateTime.Now;
	        ShoppingCartVM.OrderHeader.ApplicationUserId = userId;

	        var applicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId);

			foreach (var cart in ShoppingCartVM.ShoppingCartList)
	        {
		        cart.Price = GetPriceBasedOnQuantity(cart);
		        ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
	        }

	        if (applicationUser.CompanyId.GetValueOrDefault() == 0)
	        {
		        ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
		        ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;
	        }
	        else
	        {
		        ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
		        ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusApproved;
	        }
	        _unitOfWork.OrderHeader.Add(ShoppingCartVM.OrderHeader);
	        _unitOfWork.Save();
	        foreach (var cart in ShoppingCartVM.ShoppingCartList)
	        {
		        OrderDetail orderDetail = new()
		        {
			        ProductId = cart.ProductId,
			        OrderHeaderId = ShoppingCartVM.OrderHeader.Id,
			        Price = cart.Price,
			        Count = cart.Count
		        };
		        _unitOfWork.OrderDetail.Add(orderDetail);
		        _unitOfWork.Save();
	        }

	        if (applicationUser.CompanyId.GetValueOrDefault() == 0)
	        {
		        var domain = "https://localhost:7133/";
		        var options = new SessionCreateOptions
		        {
			        SuccessUrl = domain + $"customer/cart/OrderConfirmation?id={ShoppingCartVM.OrderHeader.Id}",
			        CancelUrl = domain + "customer/cart/index",
			        LineItems = new List<SessionLineItemOptions>(),
			        Mode = "payment",
		        };

		        foreach (var item in ShoppingCartVM.ShoppingCartList)
		        {
			        var sessionLineItem = new SessionLineItemOptions
			        {
				        PriceData = new SessionLineItemPriceDataOptions
				        {
					        UnitAmount = (long)(item.Price * 100), // $20.50 => 2050
					        Currency = "usd",
					        ProductData = new SessionLineItemPriceDataProductDataOptions
					        {
						        Name = item.Product.Title
					        }
				        },
				        Quantity = item.Count
			        };
			        options.LineItems.Add(sessionLineItem);
		        }


		        var service = new SessionService();
		        Session session = service.Create(options);
		        _unitOfWork.OrderHeader.UpdateStripePaymentId(ShoppingCartVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
		        _unitOfWork.Save();
		        Response.Headers.Add("Location", session.Url);
		        return new StatusCodeResult(303);
			}

	        return RedirectToAction(nameof(OrderConfirmation), new {id = ShoppingCartVM.OrderHeader.Id});
        }

        public IActionResult OrderConfirmation(int id)
        {
	        OrderHeader orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == id, includeProperties: "ApplicationUser");
	        if (orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment)
	        {
		        //this is an order by customer

		        var service = new SessionService();
		        Session session = service.Get(orderHeader.SessionId);

		        if (session.PaymentStatus.ToLower() == "paid")
		        {
			        _unitOfWork.OrderHeader.UpdateStripePaymentId(id, session.Id, session.PaymentIntentId);
			        _unitOfWork.OrderHeader.UpdateStatus(id, SD.StatusApproved, SD.PaymentStatusApproved);
			        _unitOfWork.Save();
		        }
	        }

	        List<ShoppingCart> shoppingCarts = _unitOfWork.ShoppingCart
		        .GetAll(u => u.ApplicationUserId == orderHeader.ApplicationUserId).ToList();

	        _unitOfWork.ShoppingCart.RemoveRange(shoppingCarts);
	        _unitOfWork.Save();

	        return View(id);
        }


		public IActionResult Plus(int cartId)
        {
            var cartFromDb = _unitOfWork.ShoppingCart.Get(x => x.Id == cartId);
            cartFromDb.Count++;
            _unitOfWork.ShoppingCart.Update(cartFromDb);
            _unitOfWork.Save();

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Minus(int cartId)
        {
            var cartFromDb = _unitOfWork.ShoppingCart.Get(x => x.Id == cartId);
            if (cartFromDb.Count <= 1) _unitOfWork.ShoppingCart.Remove(cartFromDb);
            else
            {
                cartFromDb.Count--;
                _unitOfWork.ShoppingCart.Update(cartFromDb);
            }

            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Remove(int cartId)
        {
            var cartFromDb = _unitOfWork.ShoppingCart.Get(x => x.Id == cartId);
            _unitOfWork.ShoppingCart.Remove(cartFromDb);
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }

        private double GetPriceBasedOnQuantity(ShoppingCart shoppingCart)
        {
            if (shoppingCart.Count <= 50) return shoppingCart.Product.Price;
            return shoppingCart.Count <= 100 ? shoppingCart.Product.Price50 : shoppingCart.Product.Price100;
        }
    }
}
