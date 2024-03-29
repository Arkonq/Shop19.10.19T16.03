﻿using Microsoft.Extensions.Configuration;
using Shop.DataAccess;
using Shop.Domain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/*
		1. Регистрация и вход (смс-код / email код) - сделать до 11.10 (Email есть на метаните)
		2. История покупок 
		3. Категории и товары (картинка в файловой системе)
		4. Покупка (корзина), оплата и доставка (PayPal/Qiwi/etc)
		5. Комментарии и рейтинги
		6. Поиск (пагинация - постраничность)

		Кто сделает 3 версии (Подключенный, автономный и EF) получит автомат на экзамене
*/

namespace Shop.UI
{
	class Program
	{
		static IConfigurationBuilder builder = new ConfigurationBuilder()
							.SetBasePath(Directory.GetCurrentDirectory())
							.AddJsonFile("appsettings.json", false, true);

		static IConfigurationRoot configurationRoot = builder.Build();
		static string connectionString = configurationRoot.GetConnectionString("HomeConnectionString");
		static string providerName = configurationRoot
							.GetSection("AppConfig")
							.GetChildren().Single(item => item.Key == "ProviderName")
							.Value;

		static User user = null;

		static void Main(string[] args)
		{
			Entry();

			//Search();
			//Pagination();
			//Test();
			//Registration();
			//SignIn();
		}

		static void Entry()
		{
			int entryAnswer = -1;
			while (entryAnswer != 0)
			{
				Console.Clear();
				Console.WriteLine("1. Регистрация");
				Console.WriteLine("2. Вход");
				Console.WriteLine("0. Выход");
				Console.Write("Выберите действие: ");
				if (Int32.TryParse(Console.ReadLine(), out entryAnswer) == false || entryAnswer < 0)
				{
					entryAnswer = -1;  // При выпадении false в Int32.TryParse параметр out page присваивается значние 0, и с новым циклом программа завершает работу
					WriteMessage("Введенное действие не является корректным. Выберите действие из списка!");
					continue;
				}
				switch (entryAnswer)
				{
					case 1:
						Registration();
						WriteMessage("Ваш аккаунт зарегестрирован!");
						break;
					case 2:
						if (!Authorization())
						{
							WriteMessage("Превышено кол-во попыток авторизации. Переход на главную страницу");
							continue;
						}
						WriteMessage("Вы успешно авторизовались!");
						Menu();
						break;
					case 0:
						entryAnswer = 0;
						break;
				}
			}
		}

		private static bool Authorization()
		{
			Console.Clear();
			string login, password, verCode;
			bool wrong = false;
			int attemptToLogIn = 0;
			Console.WriteLine("Введите Логин: ");
			login = Console.ReadLine();
			Console.WriteLine("Введите пароль: ");
			password = Console.ReadLine();
			using (var context = new ShopContext(connectionString))
			{
				var result = from user_
										 in context.Users
										 where user_.Login.Equals(login) & user_.Password.Equals(password)
										 select user_;
				if (result.Count() == 0)
				{
					wrong = true;
				}
			}
			if (wrong)
			{
				while (wrong && attemptToLogIn < 3)
				{
					Console.WriteLine("Ошибка! Неверный логин или пароль! Попробуйте снова.");
					Console.WriteLine($"Осталось попыток - {3 - attemptToLogIn++}");
					Console.WriteLine("Введите Логин: ");
					login = Console.ReadLine();
					Console.WriteLine("Введите пароль: ");
					password = Console.ReadLine();
					using (var context = new ShopContext(connectionString))
					{
						var result = from user_
												 in context.Users
												 where user_.Login.Equals(login) & user_.Password.Equals(password)
												 select user_;
						if (result.Count() == 0)
						{
							wrong = true;
						}
						else
						{
							wrong = false;
						}
					}
				}
			}
			if (wrong)
			{
				return false;
			}
			Console.WriteLine("Введите секретный код: ");
			verCode = Console.ReadLine();
			using (var context = new ShopContext(connectionString))
			{
				var result = from user_
										 in context.Users
										 where user_.Login.Equals(login) & user_.Password.Equals(password) & user_.VerificationCode.Equals(verCode)
										 select user_;
				if (result.Count() == 0)
				{
					wrong = true;
				}
			}
			if (wrong)
			{
				while (wrong && attemptToLogIn < 3)
				{
					Console.WriteLine("Ошибка! Неверный секретный код! Попробуйте снова.");
					Console.WriteLine($"Осталось попыток - {3 - attemptToLogIn++}");
					Console.WriteLine("Введите секретный код: ");
					verCode = Console.ReadLine();
					using (var context = new ShopContext(connectionString))
					{
						var result = from user_
										 in context.Users
												 where user_.Login.Equals(login) & user_.Password.Equals(password) & user_.VerificationCode.Equals(verCode)
												 select user_;
						if (result.Count() == 0)
						{
							wrong = true;
						}
						else
						{
							wrong = false;
						}
					}
				}
			}
			if (wrong)
			{
				return false;
			}
			using (var context = new ShopContext(connectionString))
			{
				var result = from user_
								 in context.Users
										 where user_.Login.Equals(login) & user_.Password.Equals(password) & user_.VerificationCode.Equals(verCode)
										 select user_;
				user = result.First();
			}
			return true;
		}

		private static void Menu()
		{
			int menuAnswer = -1;
			while (menuAnswer != 0)
			{
				Console.WriteLine("1. Мой аккаунт");
				Console.WriteLine("2. Поиск товаров");
				Console.WriteLine("3. Все категории и товары");
				Console.WriteLine("0. Выход");
				Console.Write("Выберите действие: ");
				if (Int32.TryParse(Console.ReadLine(), out menuAnswer) == false || menuAnswer < 0)
				{
					menuAnswer = -1;  // При выпадении false в Int32.TryParse параметр out page присваивается значние 0, и с новым циклом программа завершает работу
					WriteMessage("Введенное действие не является корректным. Выберите действие из списка!");
					continue;
				}
				switch (menuAnswer)
				{
					case 1:
						int comments;
						using (var context = new ShopContext(connectionString))
						{
							var countComments = context.Users.Join(context.Comments, // второй набор
							user => user.Id, // свойство-селектор объекта из первого набора
							comment => comment.UserId, // свойство-селектор объекта из второго набора
							(p, c) => new // результат
							{
								Name = p.FullName
							});
							comments = countComments.Count();
						}
						Console.Clear();
						Console.WriteLine("Информация о вашем аккаунте:");
						Console.WriteLine($"\tУникальный идентификатор: {user.Id}");
						Console.WriteLine($"\tДата регистрации: {user.CreationDate}");
						Console.WriteLine($"\tЛогин: {user.Login}");
						Console.WriteLine($"\tНа счету: {user.Balance}");
						Console.WriteLine($"\tПолное имя: {user.FullName}");
						Console.WriteLine($"\tНомер телефона: {user.PhoneNumber}");
						Console.WriteLine($"\tПочта: {user.Email}");
						Console.WriteLine($"\tАдрес доставки: {user.Address}");
						Console.WriteLine($"\tПароль: {user.Password}");
						Console.WriteLine($"\tСекретный код: {user.VerificationCode}");
						Console.WriteLine($"\tВсего комментариев: {comments}");
						Console.ReadLine();
						Console.Clear();
						break;
					case 2:
						Search();
						break;
					case 3:

						break;
					case 0:
						menuAnswer = 0;
						break;
				}
			}
		}

		static void ProcessCollections()
		{
			List<string> cityNames = new List<string>
			{
				"Almaty", "Ankara", "Boriswill", "Nur-Sultan", "Yalta"
			};

			List<string> processedCityNames = new List<string>(); // для поиска товаров от пользователя
			foreach (string name in cityNames)
			{
				if (name.Contains("-"))
				{
					processedCityNames.Add(name);
				}
			}

			var result = from name
									 in cityNames
									 where name.Contains("-")
									 select name;

			var shortResult = cityNames.Where(name => name.Contains("-"));
			var shortResult2 = cityNames.Select(name => name.Contains("-"));
		}

		private static void Test()
		{
			Category category = new Category
			{
				Name = "Бытовая техника",
				//ImagePath = "C:/data",
			};


			Item item = new Item
			{
				Name = "Пылесос",
				//ImagePath = "C:/data/items",
				//Price = 25999,
				//Description = "Обычный пылесос",
				CategoryId = category.Id
			};

			User user = new User
			{
				FullName = "Иван Иванов",
				PhoneNumber = "123456",
				Email = "qwer@qwr.qwr",
				Address = "Twesd, 12",
				Password = "password",
				VerificationCode = "1234"
			};

			for (int i = 0; i < 10; i++)
			{

				Comment comment = new Comment
				{
					UserId = Guid.Parse("c368194e-3f8c-4753-b39a-cceedf7b59ec"),
					ItemId = item.Id,
					Value = "Долго выбирал между Лж, Бошем и Самсунгом. Посмотрел 'Контрольную закупку' про пылесосы с мешком, и выбрал этот, с регулятором( модель SC5610 дешевле, но без регулятора мощности) Покупал за 1800, хотел именно классик, который понимает одноразовые бумажные мешки, и эти мешки есть в продаже по адекватной цене.До этого был занусси без мешка(циклон) и моющий кёрхер.Циклон оказался непрактичным, малоёмким 1, 2л, дорогим по замене фильтра.Моющий, конечно, хорош, и свежесть, и влажность повышает, но с ним много мороки: уборка занимает минут 20, а мойка и чистка самого моющего пылесоса и всех ёмкостей, мойка ванной после мойки моющего пылесоса на полчаса, иначе пахнет плесенью"
				};

				using (var context = new ShopContext(connectionString))
				{
					//context.Users.Add(user);
					//context.Items.Add(item);
					//context.Categories.Add(category);
					context.Comments.Add(comment);

					//var result = context.Categories.ToList();
					//context.Remove(category);

					//var quariedCategories = context.Categories.Where(x => x.CreationDate.Date < new System.DateTime(2017,10,5).Date);

					//var funnyResult = quariedCategories.Select(x => new
					//{
					//	Id = x.Id,
					//	StartDate = x.CreationDate,
					//	FunnyName = "Funny" + x.Name
					//});

					//var finalResult = funnyResult.ToList();

					context.SaveChanges();
				}
			}

			string data = "12345sdf";
			var newString = data.ExtractOnlyText();
		}


		static void Registration()
		{
			Console.Clear();
			string login, fullName, phoneNum, email, address, password, verCode;
			bool exist = false;
			Console.WriteLine("Введите Логин: ");
			login = Console.ReadLine();
			using (var context = new ShopContext(connectionString))
			{
				var result = from user_
										 in context.Users
										 where user_.Login.Equals(login)
										 select user_;
				if (result.Count() != 0)
				{
					exist = true;
				}
			}
			if (exist)
			{
				while (exist)
				{
					WriteMessage("Ошибка! Введеный вами логин уже существует! Попробуйте ввести другой.");
					Console.WriteLine("Введите Логин: ");
					login = Console.ReadLine();
					using (var context = new ShopContext(connectionString))
					{
						var result = from user_
												 in context.Users
												 where user_.Login.Equals(login)
												 select user_;
						if (result.Count() != 0)
						{
							exist = true;
						}
						else
						{
							exist = false;
						}
					}
				}
			}

			Console.WriteLine("Введите ФИО: ");
			fullName = Console.ReadLine();
			Console.WriteLine("Введите почту: ");
			email = Console.ReadLine();
			Console.WriteLine("Введите номер телефона: ");
			phoneNum = Console.ReadLine();
			Console.WriteLine("Введите адрес: ");
			address = Console.ReadLine();
			Console.WriteLine("Введите пароль: ");
			password = Console.ReadLine();
			Console.WriteLine("Введите секретный код (****): ");
			verCode = Console.ReadLine();


			User user = new User
			{
				Login = login,
				FullName = fullName,
				PhoneNumber = phoneNum,
				Email = email,
				Address = address,
				Password = password,
				VerificationCode = verCode
			};
			using (var context = new ShopContext(connectionString))
			{
				context.Users.Add(user);
				context.SaveChanges();
			}
		}
		static void SignIn()
		{
			string email, password;
			Console.WriteLine("Введите почту: ");
			email = Console.ReadLine();
			Console.WriteLine("Введите пароль: ");
			password = Console.ReadLine();
			using (var context = new ShopContext(connectionString))
			{
				var user = from u in context.Users
									 where u.Email.Equals(email) & u.Password.Equals(password)
									 select u;
				//Console.WriteLine(user.Count())
				if (user.Count() == 1)
				{
					Console.WriteLine("Введите секретный код: ");
				}
			}
		}

		static void Categories()
		{
			int page = 1, pageSize = 5, pages = 1, categories = 0;
			List<Category> categoriesList = null;

			using (var context = new ShopContext(connectionString))
			{
				var query = from category in context.Categories
										select category;
				categories = query.Count();
			}
			pages = categories / pageSize;
			if (categories % pageSize != 0) // Если кол-во страниц выпало как 5/3 то выйдет лишь 1 страница, поэтому добавляем еще одну
			{
				pages++;
			}

			Console.Clear();
			if (page < 0)
			{
				page = -page;
			}
			if (page > pages)
			{
				Console.WriteLine("Введенной страницы не существует.");
				Console.ReadLine();
				Console.Clear();
				return;
			}
			using (var context = new ShopContext(connectionString))
			{
				var query = from category in context.Categories
										select category;
				var paging = query.Skip((page - 1) * pageSize).Take(pageSize);
				categoriesList = paging.ToList();
			}
			Console.WriteLine($"Page {page}/{pages}:");
			int num = 1;
			foreach (var category in categoriesList)
			{
				Console.WriteLine($"\t{num++}) {category.Name}");
				List<string> itemNamesList = null;
				using (var context = new ShopContext(connectionString))
				{
					var userComment = context.Categories.Join(context.Items, // второй набор
					category => category.Id, // свойство-селектор объекта из первого набора
					item => item.CategoryId, // свойство-селектор объекта из второго набора
					(c, i) => new // результат
					{
						Name = i.Name
					});
				}
			}

		}

		static void Search()
		{
			int page = 1, pageSize = 3, pages = 1, items = 0, chooseItemOrPageAnswer, itemNum, chooseCommentsOrBuyAnswer = -1, commentsPageSize = 2, comments = 1, commentsPage = 1, commentsPages = 1;
			string search = null;
			List<Item> result = null;
			List<Comment> commentList = null;
			IQueryable<Item> query;
			while (search != "0")
			{
				page = 1; // Есть вероятность что пользователь попадет в false в Int32.TryParse параметр out page, где page присваивается -1, что вскоре приведет к ошибке
				Console.Clear();
				Console.WriteLine("Введите искомый товар (0 - Назад):");
				search = Console.ReadLine();
				if (search == "0")
				{
					continue;
				}
				using (var context = new ShopContext(connectionString))
				{
					query = from item in context.Items
									where item.Name.Contains(search)
									select item;
					items = query.Count();
				}
				if (items == 0)
				{
					WriteMessage("По вашему запросу не найдено ни одного товара.");
					continue;
				}
				pages = items / pageSize;
				if (items % pageSize != 0) // Если кол-во страниц выпало как 5/3 то выйдет лишь 1 страница, поэтому добавляем еще одну
				{
					pages++;
				}
				ShopPage(page, pageSize, pages, search, result, query);

				while (page != 0)
				{
					ShopPage(page, pageSize, pages, search, result, query);
					Console.WriteLine("0. Вернуться к поиску товаров.");
					Console.WriteLine("1. Выбрать товар.");
					Console.WriteLine("2. Выбрать страницу.");
					if (Int32.TryParse(Console.ReadLine(), out chooseItemOrPageAnswer) == false)
					{
						chooseItemOrPageAnswer = -1;  // При выпадении false в Int32.TryParse параметр out page присваивается значние 0, и с новым циклом программа завершает работу
						WriteMessage("Введенное действие не является корректным. Выберите действие из списка!");
						continue;
					}
					switch (chooseItemOrPageAnswer)
					{
						case 0:
							page = 0;
							Console.Clear();
							continue;
						case 1:
							Console.WriteLine($"Введите товар (1 - {pageSize}):");
							if (Int32.TryParse(Console.ReadLine(), out itemNum) == false || itemNum == 0)
							{
								WriteMessage("Введенное действие не является корректным. Возврат в выбор товара");
								continue;
							}
							if (itemNum < 0)
							{
								itemNum = -itemNum;
							}
							int itemsOnPage;
							using (var context = new ShopContext(connectionString))           // подсчет кол-ва записей на странице
							{
								query = from item in context.Items
												where item.Name.Contains(search)
												select item;
								query = query.Skip((page - 1) * pageSize).Take(pageSize);
								itemsOnPage = query.Count();
							}
							if (itemNum > pageSize || itemNum > itemsOnPage)
							{
								WriteMessage("Данный номер товара отсутсвует на странице.");
								continue;
							}
							while (chooseCommentsOrBuyAnswer != 0)
							{
								commentsPage = 1; // Есть вероятность что пользователь попадет в false в Int32.TryParse параметр out commentsPage, где commentsPage присваивается -1, что вскоре приведет к ошибке
								ChooseItem(itemNum, page, pageSize, search, out result, out query);
								Console.WriteLine("0. Назад");
								Console.WriteLine("1. Посмотреть комментарии");
								Console.WriteLine("2. Приобрести товар");
								if (Int32.TryParse(Console.ReadLine(), out chooseCommentsOrBuyAnswer) == false || chooseCommentsOrBuyAnswer < 0)
								{
									chooseCommentsOrBuyAnswer = -1;  // При выпадении false в Int32.TryParse параметр out page присваивается значние 0, и с новым циклом программа завершает работу
									WriteMessage("Введенное действие не является корректным. Возврат в выбор товара.");
									continue;
								}
								switch (chooseCommentsOrBuyAnswer)
								{
									case 0:
										continue;
									case 1:
										Guid resultItemId = result.First().Id;
										using (var context = new ShopContext(connectionString))
										{
											var queryCom = from comment in context.Comments
																		 where comment.ItemId.Equals(resultItemId)
																		 select comment;
											comments = queryCom.Count();
										}
										if (comments == 0)
										{
											WriteMessage("По вашему запросу не найдено ни одного комментария.");
											continue;
										}
										commentsPages = comments / commentsPageSize;
										if (comments % commentsPageSize != 0) // Если кол-во страниц выпало как 5/3 то выйдет лишь 1 страница, поэтому добавляем еще одну
										{
											commentsPages++;
										}
										CommentPage(commentsPages, commentsPageSize, commentsPage, resultItemId, commentList);
										while (commentsPage != 0)
										{
											Console.WriteLine($"Введите страницу (1 - {commentsPages}; 0 - Назад):");
											if (Int32.TryParse(Console.ReadLine(), out commentsPage) == false)
											{
												commentsPage = -1;  // При выпадении false в Int32.TryParse параметр out page присваивается значние 0, и с новым циклом программа завершает работу
												WriteMessage("Введенная страница не является числом.");
												continue;
											}
											if (commentsPage == 0)
											{
												continue;
											}
											CommentPage(commentsPages, commentsPageSize, commentsPage, resultItemId, commentList);
										}
										break;
									case 2:
										WriteMessage("Функция покупки не доступна на данный момент");
										break;
								}
							}
							break;
						case 2:
							Console.WriteLine($"Введите страницу (1 - {pages}):");
							if (Int32.TryParse(Console.ReadLine(), out page) == false)
							{
								page = -1;  // При выпадении false в Int32.TryParse параметр out page присваивается значние 0, и с новым циклом программа завершает работу
								WriteMessage("Введенная страница не является числом.");
								continue;
							}
							ShopPage(page, pageSize, pages, search, result, query);
							break;
					}

				}
			}
		}

		private static void CommentPage(int commentsPages, int commentsPageSize, int commentsPage, Guid resultItemId, List<Comment> commentList)
		{
			Console.Clear();
			if (commentsPage < 0)
			{
				commentsPage = -commentsPage;
			}
			if (commentsPage > commentsPages)
			{
				Console.WriteLine("Введенной страницы не существует.");
				Console.ReadLine();
				Console.Clear();
				return;
			}
			using (var context = new ShopContext(connectionString))
			{

				var queryCom = from comment in context.Comments
											 where comment.ItemId.Equals(resultItemId)
											 select comment;
				var paging = queryCom.Skip((commentsPage - 1) * commentsPageSize).Take(commentsPageSize);
				commentList = paging.ToList();
			}
			Console.WriteLine($"Page {commentsPage}/{commentsPages}:");

			string UserName;
			using (var context = new ShopContext(connectionString))
			{
				var userComment = context.Users.Join(context.Comments, // второй набор
				first => first.Id, // свойство-селектор объекта из первого набора
				second => second.UserId, // свойство-селектор объекта из второго набора
				(p, c) => new // результат
				{
					Name = p.FullName
				});
				UserName = userComment.First().Name;
			}
			int num = 1;
			foreach (var comment in commentList)
			{
				Console.WriteLine($"{num++})Пользователь {UserName} оставил комментарий ({comment.Id}):");
				Console.Write("\t");
				Console.WriteLine(DivideComment(comment.Value));
			}
		}

		private static string DivideComment(string str)
		{
			String[] sublines = str.Split(' ');
			str = null;
			int length = 80; //длина разбиения
			int j = 0;
			for (int i = 0; i < sublines.Count(); i++)
			{
				if (j + sublines[i].Length < length)
				{
					str = str + sublines[i] + " ";
					j = j + sublines[i].Length;
				}
				else
				{
					j = 0;
					str = str + "\r\n\t";
					i--;
				}
			}
			return str;
		}

		private static void WriteMessage(string message)
		{
			Console.Clear();
			Console.WriteLine(message);
			Console.ReadLine();
			Console.Clear();
		}

		private static void ChooseItem(int toSkip, int page, int pageSize, string search, out List<Item> result, out IQueryable<Item> query)
		{
			Console.Clear();
			toSkip--; // Если мы берем 1ый Айтем, то нужно сделать 0 скипов на странице, так как сразу берется(Take) первый же, и так далее
			using (var context = new ShopContext(connectionString))
			{
				query = from item in context.Items
								where item.Name.Contains(search)
								select item;
				query = query.Skip((page - 1) * pageSize).Take(pageSize).Skip(toSkip).Take(1);
				result = query.ToList();
			}
			foreach (var item in result)
			{
				Console.WriteLine($"\tНаименование: {item.Name}");
				Console.WriteLine($"\tИзображение: {item.ImagePath}");
				Console.WriteLine($"\tУникальный идентификатор: {item.Id}");
				Console.WriteLine($"\tЦена: {item.Price}");
				Console.WriteLine($"\tОписание: {item.Description}");
				Console.WriteLine($"\tКомментарии - (кол-во комментов)");
			}
		}

		private static void ShopPage(int page, int pageSize, int pages, string search, List<Item> result, IQueryable<Item> query)
		{
			Console.Clear();
			if (page < 0)
			{
				page = -page;
			}
			if (page > pages)
			{
				Console.WriteLine("Введенной страницы не существует.");
				Console.ReadLine();
				Console.Clear();
				return;
			}
			using (var context = new ShopContext(connectionString))
			{
				query = from item in context.Items
								where item.Name.Contains(search)
								select item;
				var paging = query.Skip((page - 1) * pageSize).Take(pageSize);
				result = paging.ToList();
			}
			Console.WriteLine($"Page {page}/{pages}:");
			int num = 1;
			foreach (var item in result)
			{
				Console.WriteLine($"\t{num++}) {item.Name} - {item.Id}");
			}
		}

		static void Pagination()
		{
			int page = 0, pageSize = 3;
			List<Item> result;
			while (page != 0)
			{
				Console.WriteLine("Введите страницу (0 для выхода):");

				if (!Int32.TryParse(Console.ReadLine(), out page))
				{
					Console.WriteLine("Введенная страница не является числом.");
					Console.ReadLine();
					Console.Clear();
					continue;
				}
				Console.Clear();
				using (var context = new ShopContext(connectionString))
				{
					var query = from item in context.Items
											orderby item.Name
											select item;

					var paging = query.Skip((page - 1) * pageSize).Take(pageSize);

					result = paging.ToList();
				}
				Console.WriteLine($"Page {page}:");
				foreach (var item in result)
				{
					Console.WriteLine($"\t{item.Name} - {item.Price} тг");
				}
			}
		}
	}
}