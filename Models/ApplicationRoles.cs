namespace FamilyHub.Models;

public static class ApplicationRoles
{
    public const string AdminLegacy = "Admin";
    public const string UserLegacy = "User";
    public const string Administrator = "Administrator";
    public const string Customer = "Customer";

    public const string AdminRoles = AdminLegacy + "," + Administrator;
    public const string CustomerRoles = UserLegacy + "," + Customer;

    public static bool IsAdministratorRole(string? roleName)
    {
        return string.Equals(roleName, Administrator, StringComparison.OrdinalIgnoreCase)
            || string.Equals(roleName, AdminLegacy, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsCustomerRole(string? roleName)
    {
        return string.Equals(roleName, Customer, StringComparison.OrdinalIgnoreCase)
            || string.Equals(roleName, UserLegacy, StringComparison.OrdinalIgnoreCase);
    }

    public static string GetDisplayRole(string? roleName)
    {
        if (IsAdministratorRole(roleName))
        {
            return Administrator;
        }

        if (IsCustomerRole(roleName))
        {
            return Customer;
        }

        return roleName ?? string.Empty;
    }
}
