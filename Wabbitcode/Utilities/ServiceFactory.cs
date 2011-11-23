﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Revsoft.Wabbitcode.Interface;
using System.Reflection;

namespace Revsoft.Wabbitcode.Utilities
{
	public class ServiceFactory : AbstractServiceFactory
	{
		private static AbstractServiceFactory instance = new ServiceFactory();
		public static AbstractServiceFactory Instance
		{
			get { return instance; }
		}
		
		List<IService> listFactory;

		public ServiceFactory()
		{
			listFactory = new List<IService>();
		}

		/// <summary>
		/// Returns an instance of the service type T
		/// </summary>
		/// <typeparam name="T">Instance of IService</typeparam>
		/// <returns>IService instance of the specified type</returns>
		public override T GetServiceInstance<T>()
		{
			return GetServiceInstance<T>(new object[1]);
		}

		/// <summary>
		/// Returns an instance of the service type T
		/// </summary>
		/// <typeparam name="T">Instance of IService</typeparam>
		/// <returns>IService instance of the specified type</returns>
		public override T GetServiceInstance<T>(params Object[] objects)
		{
			if (typeof(T).GetInterface("IService") == null)
			{
				throw new ArgumentException("Invalid service type");
			}
			var foundService = listFactory.FirstOrDefault(service => service is T);
			if (foundService == null)
			{
				foundService = (IService) CreateServiceInstance<T>(objects);
			}
			return (T) foundService;
		}

		/// <summary>
		/// Creates and initializes an instance of type T
		/// </summary>
		/// <typeparam name="T">Type of service to initialize</typeparam>
		/// <returns>Instance of the service created</returns>
		public override T CreateServiceInstance<T>()
		{
			return CreateServiceInstance<T>(new object[1]);
		}


		/// <summary>
		/// Creates and initializes an instance of type T
		/// </summary>
		/// <typeparam name="T">Type of service to initialize</typeparam>
		/// <param name="objects">default objects to pass to the service</param>
		/// <returns>Instance of the service created</returns>
		public override T CreateServiceInstance<T>(params Object[] objects)
		{
			if (typeof(T).GetInterface("IService") == null)
			{
				throw new ArgumentException("Invalid service type");
			}
			T newInstance = Activator.CreateInstance<T>();
			IService newService = (IService)newInstance;
			listFactory.Add(newService);

			var attributesList = typeof(T).GetCustomAttributes(true).Where(d => d is Services.ServiceDependencyAttribute);
			foreach (var dependency in attributesList)
			{
				string dependsName = ((Services.ServiceDependencyAttribute)dependency).DependencyName;
				Type serviceType = Type.GetType("Revsoft.Wabbitcode.Services." + dependsName);
				var foundService = listFactory.FirstOrDefault(service => service.GetType() == serviceType);
				if (foundService == null)
				{
					foundService = (IService) Activator.CreateInstance(serviceType, true);
					listFactory.Add(foundService);
				}
				var field = typeof(T).GetField(char.ToLower(dependsName[0]) + dependsName.Substring(1), BindingFlags.NonPublic | BindingFlags.Instance);
				if (field == null) {
					throw new MissingFieldException();
				}
				field.SetValue(newService, foundService);
			}
			newService.InitService(objects);
			return newInstance;
		}

		/// <summary>
		/// Destroys the instance of the service passed in
		/// </summary>
		/// <param name="serviceToDestroy">Service instance to destroy</param>
		public override void DestroyServiceInstance(IService serviceToDestroy)
		{
			listFactory.Remove(serviceToDestroy);
			serviceToDestroy.DestroyService();
		}
	}
}