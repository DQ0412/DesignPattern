using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OrganicFoodMVC.DataAccess.Data;
using OrganicFoodMVC.DataAccess.Repository;
using OrganicFoodMVC.DataAccess.Repository.IRepository;
using OrganicFoodMVC.Utility;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OrganicFoodMVC
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        public IConfiguration Configuration { get; }
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"), b => b.MigrationsAssembly("OrganicFoodMVC")));
            services.AddIdentity<IdentityUser, IdentityRole>().AddDefaultTokenProviders().AddEntityFrameworkStores<ApplicationDbContext>();

            //thêm EmailSender
            services.AddSingleton<IEmailSender, EmailSender>();
            services.Configure<EmailOptions>(Configuration);

            //thanh toán bằng stripe
            services.Configure<StripeSettings>(Configuration.GetSection("Stripe"));

            //(me) add UnitOfWork
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddControllersWithViews().AddRazorRuntimeCompilation();
            services.AddRazorPages();

            //add
            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = $"/Identity/Account/Login";
                options.LogoutPath = $"/Identity/Account/Logout";
                options.AccessDeniedPath = $"/Identity/Account/AccessDenied";
            });

            //đăng nhập facebook
            services.AddAuthentication().AddFacebook(options =>
            {
                options.AppId = "1472063540030517";
                options.AppSecret = "6f2153b5c4dacff20ebe74eb48c0d031";
            });

            //đăng nhập google
            services.AddAuthentication().AddGoogle(options =>
            {
                options.ClientId = "85031248554-294muieersq6regeieves0bj08ke1tcn.apps.googleusercontent.com";
                options.ClientSecret = "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";
            });

            //cấu hình session
            services.AddSession(options =>
            {
                //quá 30 phút thì không hoạt động thì login lại
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

        }
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                //app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            //session
            app.UseSession();

            //stripe
            StripeConfiguration.ApiKey = Configuration.GetSection("Stripe")["SecretKey"];

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    //pattern : "{area:exists}/{controller=Home}/{action=Index}/{id?}"
                    pattern: "{area=Customer}/{controller=Home}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
            });
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
