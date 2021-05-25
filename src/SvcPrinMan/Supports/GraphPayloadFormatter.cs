

using Microsoft.Graph;
using System.Text;

namespace SvcPrinMan.Supports
{
    public class GraphPayloadFormatter
    {
        public static string GetStringRepresentation(ServicePrincipal sp)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Name: {sp.DisplayName} ");
            sb.AppendLine($"Description: {sp.Description} ");
            sb.AppendLine($"AppDisplayName: {sp.AppDisplayName} ");
            sb.AppendLine($"AppDescription: {sp.AppDescription} ");

            sb.AppendLine($"Alternative names:  ");
            foreach (var name in sp.AlternativeNames)
            {
                sb.AppendLine($"\t > {name} ");
            }

            sb.AppendLine($"App Id: {sp.AppId} ");
            sb.AppendLine($"Object Id: {sp.Id} ");
            sb.AppendLine($"AppOwnerOrganizationId: {sp.AppOwnerOrganizationId} ");

            if (sp.Owners != null)
            {
                sb.AppendLine($"Owners:  ");
                foreach (var owner in sp.Owners)
                {
                    sb.AppendLine($"\t > {owner.Id} ");
                }
            }

            sb.AppendLine($"Key Credentials:  ");
            foreach (var creds in sp.KeyCredentials)
            {
                sb.AppendLine($"\t > {creds.DisplayName}; Type: {creds.Type}; Start Time: {creds.StartDateTime}; End Time: {creds.EndDateTime}");
            }

            sb.AppendLine($"Password Credentials:  ");
            foreach (var creds in sp.PasswordCredentials)
            {
                sb.AppendLine($"\t > {creds.DisplayName}; Start Time: {creds.StartDateTime}; End Time: {creds.EndDateTime}; ID: {creds.KeyId}");
            }
            return sb.ToString();
        }

        public static string GetStringRepresentation(Application app)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Name: {app.DisplayName} ");
            sb.AppendLine($"Description: {app.Description} ");

            if (app.Owners != null)
            {
                sb.AppendLine($"Owners:  ");
                foreach (var owner in app.Owners)
                {
                    sb.AppendLine($"\t > {owner.Id} ");
                }
            }

            sb.AppendLine($"App Id: {app.AppId} ");
            sb.AppendLine($"Object Id: {app.Id} ");

            if (app.Tags != null)
            {
                sb.AppendLine($"Tags:  ");
                foreach (var tag in app.Tags)
                {
                    sb.AppendLine($"\t > {tag}");
                }
            }

            if (app.AppRoles != null)
            {
                sb.AppendLine($"App Roles:  ");
                foreach (var role in app.AppRoles)
                {
                    sb.AppendLine($"\t > {role.DisplayName}; Id={role.Id}; Enabled={role.IsEnabled}; ");
                }
            }

            if (app.KeyCredentials != null)
            {
                sb.AppendLine($"Key Credentials:  ");
                foreach (var creds in app.KeyCredentials)
                {
                    sb.AppendLine($"\t > {creds.DisplayName}; Type: {creds.Type}; Start Time: {creds.StartDateTime}; End Time: {creds.EndDateTime}");
                }
            }


            if (app.PasswordCredentials != null)
            {
                sb.AppendLine($"Password Credentials:  ");
                foreach (var creds in app.PasswordCredentials)
                {
                    sb.AppendLine($"\t > {creds.DisplayName}; Start Time: {creds.StartDateTime}; End Time: {creds.EndDateTime}; ID: {creds.KeyId}");
                }
            }
            return sb.ToString();
        }
        public static string GetStringRepresentation(PasswordCredential passCred)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{passCred.DisplayName} ({passCred.StartDateTime} - {passCred.EndDateTime})");
            sb.AppendLine($"Password: {passCred.SecretText}");
            return sb.ToString();
        }
    }
}
