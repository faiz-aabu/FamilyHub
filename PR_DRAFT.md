PR Title: Fix: family relationships EF translation + add ModelState logging

Summary:
Fixes server-side EF translation errors in relationship validation and improves diagnostics.

Changes:
- `Controllers/FamilyRelationshipsController.cs`: inject `ILogger`, log `ModelState` errors, and catch/log unexpected exceptions with a user-friendly message.
- `Services/FamilyRelationshipService.cs`: replace non-translatable LINQ/string operations with EF-friendly predicates to allow `AnyAsync` to translate to SQL.

Why:
Prevents runtime InvalidOperationException caused by using .NET-only string comparison overloads inside EF queries and makes validation failures easier to debug.

Testing performed:
- Rebuilt and ran the app locally.
- Verified relationship creation end-to-end (created Test User ↔ Muhammad Abubakar).
- Verified Dashboard, Family Members, Family Tree, Reports (Excel export), Notifications, Manage Users, Search, and Activity Logs render correctly.

Notes / Next steps:
- Commit changes locally and push branch: `git add -A && git commit -m "Fix: family relationships EF translation + add ModelState logging" && git push origin HEAD`.
- Optionally create a branch and open a PR on GitHub with this description.

Signed-off-by: Automated assistant
