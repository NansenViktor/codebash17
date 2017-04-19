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
using System.Collections.Generic;
using EPiServer.Web.Routing;

namespace CodeBash2017.Controllers
{
	public class StartController : PageController<StartPage>
	{
		private readonly Injected<IContentRepository> _contentRepository;
		private readonly Injected<SiteDefinition> _siteDefinition;

		private static Random _random = new Random();

		public ActionResult Index(StartPage page, int level =1)
		{
			var allAnimalImages = Images;
			// Picking a random one
			var randomAnimalIndex = new Random().Next(allAnimalImages.Count);
			var startPageViewModel = new StartPageViewModel
			{
				Image = allAnimalImages[randomAnimalIndex],
				TagImages = Load(level),
				TopList = GenerateTopList().ToList(),
				Seed = _random.Next(4)
			};
			return View(startPageViewModel);
		}


		public IList<TagImageViewModel> Load(int level)
		{
			var images = (List<int>)Session["UsedImages"];
			if (images == null)
			{
				images = new List<int>();
				Session["UsedImages"] = images;
			}
			var allImages = Images;
			var imagesToSend = new List<AnimalImage>();
			while (imagesToSend.Count < 4)
			{
				var index = _random.Next(allImages.Count());
				if (!images.Contains(index))
				{
					var tempImage=allImages[index];
					if (tempImage.Tags != null && tempImage.Tags.Count > 3)
					{
						imagesToSend.Add(tempImage);
						images.Add(index);
					}
				}
			}
			var model = new List<TagImageViewModel>();

			foreach (var image in imagesToSend)
			{
				var levelRange = image.Tags.Count / 3;
				var no = _random.Next(levelRange);
				var selectedTag = image.Tags.ElementAt(levelRange * level + no);

				model.Add(new TagImageViewModel
				{
					Url = UrlResolver.Current.GetUrl(image),
					Tag = selectedTag
				});
			}
			return model;
		}

		public ActionResult Submit(string emailAddress, int score)
		{
			var page = _contentRepository.Service.Get<StartPage>(ContentReference.StartPage);
			var topList = page.TopList.Split(new char[] { '§' }, StringSplitOptions.RemoveEmptyEntries).ToList();
			topList.Add($"{emailAddress}|{topList}");
			var writePage = (StartPage)page.CreateWritableClone();
			writePage.TopList = string.Join("§", topList);
			_contentRepository.Service.Save(writePage, EPiServer.DataAccess.SaveAction.Publish);
			return RedirectToAction("Index");
		}

		//public ActionResult TopList()
		//{
		//	//return GenerateTopList();
		//}

		private IEnumerable<ScoreModel> GenerateTopList()
		{
			var page = _contentRepository.Service.Get<StartPage>(ContentReference.StartPage);
			var topList = page.TopList?.Split(new char[] { '§' }, StringSplitOptions.RemoveEmptyEntries).ToList()
				.Select(item => new ScoreModel
				{
					Email = item.Split('|')[0],
					Score = int.Parse(item.Split('|')[1])
				}) ?? new List<ScoreModel>();
			return topList;
		}

		private IList<AnimalImage> Images
		{
			get

			{
				var mediaFolder = _contentRepository.Service.GetChildren<ContentFolder>(_siteDefinition.Service.GlobalAssetsRoot)
		.FirstOrDefault(c => c.Name.Equals(ImportImagesJob.ImageFolder));

				// Get all the animal images (there's over 1300 of them!)
				var allAnimalImages = _contentRepository.Service.GetChildren<AnimalImage>(mediaFolder.ContentLink).ToList();
				return allAnimalImages;
			}
		}

	}


	public class ScoreModel
	{
		public string Email { get; set; }
		public int Score { get; set; }
	}

	public class TagImageViewModel
	{
		public string Url { get; set; }
		public Tag Tag { get; set; }
	}

	public class StartPageViewModel
	{
		public StartPage Page { get; set; }
		public AnimalImage Image { get; set; }
		public IList<TagImageViewModel> TagImages { get; set; } 
		public IList<ScoreModel> TopList { get; set; }
		public int Seed { get; set; }
	}
}