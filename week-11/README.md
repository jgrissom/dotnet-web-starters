# Week 11 lab starter — The Registry gets members

**Copy this folder out** to wherever you keep your projects, then work on the copy.
Never work inside the clone: the next `git pull` will fight you.

## The instructions are not in here

They live in the course repo, which you read in your browser — nothing to download:

👉 **[Week 11 lab instructions](https://github.com/jgrissom/dotnet-web-dev/blob/main/week-11/lab/README.md)**

Everything else for the week — lecture notes, homework, slides — is in the same place:

👉 **[Week 11 index](https://github.com/jgrissom/dotnet-web-dev/blob/main/week-11/README.md)**

## What's already done for you

This is week 9's finished Registry, plus the three fixes week 10's midterm made to your own app. Week 10 had no lab, so the Registry never got them — they're in here so you start from a finished site, not so you can practice them:

- **Privacy is gone**, from all four places: the navbar link, the footer link, the `Privacy()` action and `Views/Home/Privacy.cshtml`.
- **A wrong id lands somewhere.** `Views/Home/Missing.cshtml`, a `Missing()` action on `HomeController`, and `app.UseStatusCodePagesWithReExecute("/Home/Missing")` in `Program.cs`. Try `/Cryptids/Details/999`.
- **`/Cryptids` says *1 creature*, not *1 creatures*.**

None of tonight's work touches them.
