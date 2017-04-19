using EPiServer.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using EPiServer.Framework.Initialization;
using EPiServer.Shell.Security;
using EPiServer.DataAbstraction;
using EPiServer.ServiceLocation;
using EPiServer.Cms.UI.AspNetIdentity;
using Microsoft.AspNet.Identity.EntityFramework;
using EPiServer.Security;
using EPiServer.Core;

namespace CodeBash2017.Business.Internal
{
    [ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
    internal class CreateAdminUserModule : IInitializableModule
    {
        UIUserProvider _userProvider;
        UIRoleProvider _roleProvider;

        public void Initialize(InitializationEngine context)
        {
            // Assume that everything is setup if the WebAdmins role has been created
            if (UIRoleProvider.RoleExists("WebAdmins"))
            {
                return;
            }

            AddUsersAndRoles(context.Locate.Advanced.GetInstance<IContentSecurityRepository>());
        }

        private void AddUsersAndRoles(IContentSecurityRepository securityRepository)
        {
            var password = "sparr0wHawk";

            AddRole("WebAdmins", AccessLevel.FullAccess, securityRepository);
            AddRole("WebEditors", AccessLevel.FullAccess ^ AccessLevel.Administer, securityRepository);

            AddUser("cmsadmin", "Administrator Administrator", password, new[] { "WebEditors", "WebAdmins" });
        }

        private void AddUser(string userName, string fullName, string passWord, string[] roleNames)
        {
            if (UIUserProvider.GetUser(userName) == null)
            {
                var email = string.Format("epic-{0}@mailinator.com", userName);
                IEnumerable<string> erros;
                UIUserCreateStatus status;
                var user = UIUserProvider.CreateUser(userName, passWord, email, null, null, true, out status, out erros);
                UIRoleProvider.AddUserToRoles(user.Username, roleNames);
            }
        }

        private void AddRole(string roleName, AccessLevel accessLevel, IContentSecurityRepository securityRepository)
        {
            if (!UIRoleProvider.RoleExists(roleName))
            {
                UIRoleProvider.CreateRole(roleName);

                var permissions = (IContentSecurityDescriptor)securityRepository.Get(ContentReference.RootPage).CreateWritableClone();
                permissions.AddEntry(new AccessControlEntry(roleName, accessLevel));

                securityRepository.Save(ContentReference.RootPage, permissions, SecuritySaveType.Replace);
                securityRepository.Save(ContentReference.WasteBasket, permissions, SecuritySaveType.Replace);
            }
        }

        public void Uninitialize(InitializationEngine context)
        {
        }

        public UIUserProvider GetDeafultUserProvider()
        {
            UIUserProvider userProvider = null;
            try
            {
                // Owin is not configured that becuase we have catch (membership provider  there is not problem)
                ServiceLocator.Current.TryGetExistingInstance<UIUserProvider>(out userProvider);
                return userProvider;
            }
            catch { }

            // in the case of aspnet identity the rpovider is not in the service locator before owin is sets up then we create own.
            var userManager = new ApplicationUserManager<ApplicationUser>(new UserStore<ApplicationUser>(new ApplicationDbContext<ApplicationUser>("EPiServerDB")));
            userProvider = new ApplicationUserProvider<ApplicationUser>(() => userManager);
            return userProvider;
        }

        public UIRoleProvider GetDeafultRoleProvider()
        {
            UIRoleProvider roleProvider = null;
            try
            {
                // Owin is not configured that becuase we have catch (membership provider  there is not problem)
                ServiceLocator.Current.TryGetExistingInstance<UIRoleProvider>(out roleProvider);
                return roleProvider;
            }
            catch { }

            // in the case of aspnet identity the rpovider is not in the service locator before owin is sets up then we create own.
            var userManager = new ApplicationUserManager<ApplicationUser>(new UserStore<ApplicationUser>(new ApplicationDbContext<ApplicationUser>("EPiServerDB")));
            var roleManager = new ApplicationRoleManager<ApplicationUser>(new RoleStore<IdentityRole>(new ApplicationDbContext<ApplicationUser>("EPiServerDB")));
            roleProvider = new ApplicationRoleProvider<ApplicationUser>(() => userManager, () => roleManager);
            return roleProvider;
        }

        UIUserProvider UIUserProvider
        {
            get
            {
                return _userProvider ?? (_userProvider = GetDeafultUserProvider());
            }
        }

        UIRoleProvider UIRoleProvider
        {
            get
            {
                return _roleProvider ?? (_roleProvider = GetDeafultRoleProvider());
            }
        }
    }
}