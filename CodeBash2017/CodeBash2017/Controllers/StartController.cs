/*

    WELCOME TO...
     ___________ _____   _____ ___________ _____  ______  ___   _____ _   _   _____  _____  __   ______
    |  ___| ___ \_   _| /  __ \  _  |  _  \  ___| | ___ \/ _ \ /  ___| | | | / __  \|  _  |/  | |___  /
    | |__ | |_/ / | |   | /  \/ | | | | | | |__   | |_/ / /_\ \\ `--.| |_| | `' / /'| |/' |`| |    / /
    |  __||  __/  | |   | |   | | | | | | |  __|  | ___ \  _  | `--. \  _  |   / /  |  /| | | |   / /
    | |___| |    _| |_  | \__/\ \_/ / |/ /| |___  | |_/ / | | |/\__/ / | | | ./ /___\ |_/ /_| |_./ /
    \____/\_|    \___/   \____/\___/|___/ \____/  \____/\_| |_/\____/\_| |_/ \_____/ \___/ \___/\_/

    Kolmården is celebrating 52 years and wants a new and fun landing page.
    

    FILE INFO:
    This file has the controller and view model for the landing page.
    
    WHAT THIS FILE DOES:
    To help you get started it gets all animal photos and picks one on random.
    
    WHAT YOU SHOULD DO:
    1) Read through this controller and view model
    2) You probably want to select animals based on the metadata available on its properties instead. Be creative!
    3) Take a look at view Index.cshtml

    OTHER FILES:
    The view is in Views/Start/Index.cshtml

 */



using System;
using System.Linq;
using System.Web.Mvc;
using CodeBash2017.Business.Internal;
using CodeBash2017.Models.Media;
using CodeBash2017.Models.Pages;
using EPiServer;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using EPiServer.Web.Mvc;

namespace CodeBash2017.Controllers
{
    public class StartController : PageController<StartPage>
    {
        private readonly Injected<IContentRepository> _contentRepository;
        private readonly Injected<SiteDefinition> _siteDefinition;

        public ActionResult Index(StartPage page)
        {
            var mediaFolder = _contentRepository.Service.GetChildren<ContentFolder>(_siteDefinition.Service.GlobalAssetsRoot)
                .FirstOrDefault(c => c.Name.Equals(ImportImagesJob.ImageFolder));

            // Get all the animal images (there's over 1300 of them!)
            var allAnimalImages = _contentRepository.Service.GetChildren<AnimalImage>(mediaFolder.ContentLink).ToList();

            // Picking a random one
            var randomAnimalIndex = new Random().Next(allAnimalImages.Count);
            var startPageViewModel = new StartPageViewModel
            {
                Image = allAnimalImages[randomAnimalIndex]
            };

            return View(startPageViewModel);
        }

        public ActionResult Dummy()
        {
            return View();
        }
    }

    public class StartPageViewModel
    {
        public StartPage Page { get; set; }

        public AnimalImage Image { get; set; }
    }
}