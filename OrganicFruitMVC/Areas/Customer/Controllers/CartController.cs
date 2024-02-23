using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using OrganicFoodMVC.DataAccess.Repository.IRepository;
using OrganicFoodMVC.Models;
using OrganicFoodMVC.Models.ViewModels;
using OrganicFoodMVC.Utility;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace OrganicFoodMVC.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class CartController : Controller
    {

        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailSender _emailSender;

        private readonly UserManager<IdentityUser> _userManager;

        [BindProperty]
        public ShoppingCartVM ShoppingCartVM { get; set; }

        public CartController(IUnitOfWork unitOfWork, IEmailSender emailSender,
            UserManager<IdentityUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _emailSender = emailSender;
            _userManager = userManager;
        }

        [Authorize]
        public IActionResult Index()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);


            ShoppingCartVM = new ShoppingCartVM()
            {
                OrderHeader = new Models.OrderHeader(),
                ListCart = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == claim.Value, includeProperties: "Product")
            };
            ShoppingCartVM.OrderHeader.OrderTotal = 0;
            ShoppingCartVM.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUser
                                                            .GetFirstOrDefault(u => u.Id == claim.Value, includeProperties: "Company");

            foreach(var list in ShoppingCartVM.ListCart)
            {
                list.Price = list.Product.Price;

                // total price
                ShoppingCartVM.OrderHeader.OrderTotal += (list.Price * list.Count);

                // nếu mô tả sp dài quá thì cắt đi
                if (list.Product.Discription.Length > 100)
                {
                    list.Product.Discription = list.Product.Discription.Substring(0, 99) + "...";
                }
            }

            return View(ShoppingCartVM);
        }
        
        // verification email cart
        [HttpPost]
        [ActionName("Index")]
        public async Task<IActionResult> IndexPOST()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            var user = _unitOfWork.ApplicationUser.GetFirstOrDefault(u => u.Id == claim.Value);

            if(user == null)
            {
                ModelState.AddModelError(string.Empty, "Không tồn tại!");
            }

            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { area = "Identity", userId = user.Id, code = code },
                protocol: Request.Scheme);

            await _emailSender.SendEmailAsync(user.Email, "Xác nhận email",
                $"Vui lòng xác nhận tài khoản của bạn bằng cách <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>Bấm vào đây</a>.");

            ModelState.AddModelError(string.Empty, "Kiểm tra email đến. Hãy kiểm tra email của bạn");
            return RedirectToAction("Index");
        }

        // change quantity 
        //plus
        public IActionResult Plus(int cartId)
        {
            var cart = _unitOfWork.ShoppingCart.GetFirstOrDefault
                            (c => c.Id == cartId, includeProperties: "Product");
            cart.Count += 1;
            cart.Price = cart.Product.Price;
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }

        //Minus
        public IActionResult Minus(int cartId)
        {
            var cart = _unitOfWork.ShoppingCart.GetFirstOrDefault
                            (c => c.Id == cartId, includeProperties: "Product");

            if (cart.Count == 1)
            {
                var cnt = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == cart.ApplicationUserId).ToList().Count;
                _unitOfWork.ShoppingCart.Remove(cart);
                _unitOfWork.Save();
                HttpContext.Session.SetInt32(SD.ssShoppingCart, cnt - 1);
            }
            else
            {
                cart.Count -= 1;
                cart.Price = cart.Product.Price;
                _unitOfWork.Save();
            }

            return RedirectToAction(nameof(Index));
        }

        //remove
        public IActionResult Remove(int cartId)
        {
            var cart = _unitOfWork.ShoppingCart.GetFirstOrDefault
                            (c => c.Id == cartId, includeProperties: "Product");

            var cnt = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == cart.ApplicationUserId).ToList().Count;
            _unitOfWork.ShoppingCart.Remove(cart);
            _unitOfWork.Save();
            HttpContext.Session.SetInt32(SD.ssShoppingCart, cnt - 1);


            return RedirectToAction(nameof(Index));
        }

        //summary
        public IActionResult Summary()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            ShoppingCartVM = new ShoppingCartVM()
            {
                OrderHeader = new Models.OrderHeader(),
                ListCart = _unitOfWork.ShoppingCart.GetAll(c => c.ApplicationUserId == claim.Value,
                                                            includeProperties: "Product")
            };

            ShoppingCartVM.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUser
                                                            .GetFirstOrDefault(c => c.Id == claim.Value,
                                                                includeProperties: "Company");

            foreach (var list in ShoppingCartVM.ListCart)
            {
                list.Price = list.Product.Price;

                ShoppingCartVM.OrderHeader.OrderTotal += (list.Price * list.Count);
            }
            ShoppingCartVM.OrderHeader.Name = ShoppingCartVM.OrderHeader.ApplicationUser.Name;
            ShoppingCartVM.OrderHeader.PhoneNumber = ShoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber;
            ShoppingCartVM.OrderHeader.StreetAddress = ShoppingCartVM.OrderHeader.ApplicationUser.StreetAddress;
            ShoppingCartVM.OrderHeader.City = ShoppingCartVM.OrderHeader.ApplicationUser.City;
            ShoppingCartVM.OrderHeader.Village = ShoppingCartVM.OrderHeader.ApplicationUser.Village;
            ShoppingCartVM.OrderHeader.District = ShoppingCartVM.OrderHeader.ApplicationUser.District;

            return View(ShoppingCartVM);
        }

        // pay pay
        [HttpPost]
        [ActionName("Summary")]
        [ValidateAntiForgeryToken]
        public IActionResult SummaryPost(string stripeToken)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            ShoppingCartVM.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUser
                                                            .GetFirstOrDefault(c => c.Id == claim.Value,
                                                                    includeProperties: "Company");

            ShoppingCartVM.ListCart = _unitOfWork.ShoppingCart
                                        .GetAll(c => c.ApplicationUserId == claim.Value,
                                        includeProperties: "Product");

            ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
            ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;
            ShoppingCartVM.OrderHeader.ApplicationUserId = claim.Value;
            ShoppingCartVM.OrderHeader.OrderDate = DateTime.Now;

            _unitOfWork.OrderHeader.Add(ShoppingCartVM.OrderHeader);
            _unitOfWork.Save();

            foreach (var item in ShoppingCartVM.ListCart)
            {
                item.Price = item.Product.Price;
                OrderDetails orderDetails = new OrderDetails()
                {
                    ProductId = item.ProductId,
                    OrderId = ShoppingCartVM.OrderHeader.Id,
                    Price = item.Price,
                    Count = item.Count
                };
                ShoppingCartVM.OrderHeader.OrderTotal += orderDetails.Count * orderDetails.Price;
                _unitOfWork.OrderDetails.Add(orderDetails);

            }

            _unitOfWork.ShoppingCart.RemoveRange(ShoppingCartVM.ListCart);
            _unitOfWork.Save();
            HttpContext.Session.SetInt32(SD.ssShoppingCart, 0);

            if (stripeToken == null)
            {
                //order will be created for delayed payment for authroized company
                ShoppingCartVM.OrderHeader.PaymentDueDate = DateTime.Now.AddDays(30);
                ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
                ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusApproved;
            }
            else
            {
                //process the payment
                var options = new ChargeCreateOptions
                {
                    Amount = Convert.ToInt32(ShoppingCartVM.OrderHeader.OrderTotal),
                    Currency = "vnd",
                    Description = "Order ID : " + ShoppingCartVM.OrderHeader.Id,
                    Source = stripeToken
                };

                var service = new ChargeService();
                Charge charge = service.Create(options);

                if (charge.Id == null)
                {
                    ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusRejected;
                }
                else
                {
                    ShoppingCartVM.OrderHeader.TransactionId = charge.Id;
                }
                if (charge.Status.ToLower() == "succeeded")
                {
                    ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusApproved;
                    ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusApproved;
                    ShoppingCartVM.OrderHeader.PaymentDate = DateTime.Now;
                }
            }

            _unitOfWork.Save();

            return RedirectToAction("OrderConfirmation", "Cart", new { id = ShoppingCartVM.OrderHeader.Id });

        }

        //Order Confirmation
        public IActionResult OrderConfirmation(int id)
        {
            //twilio dùng để gửi tin nhắn nhưng lỗi
           /* OrderHeader orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == id);
            TwilioClient.Init(_twilioOptions.AccountSid, _twilioOptions.AuthToken);
            try
            {
                var message = MessageResource.Create(
                    body: "Order Placed on Bulky Book. Your Order ID:" + id,
                    from: new Twilio.Types.PhoneNumber(_twilioOptions.PhoneNumber),
                    to: new Twilio.Types.PhoneNumber(orderHeader.PhoneNumber)
                    );
            }
            catch (Exception ex)
            {

            }*/



            return View(id);
        }

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
