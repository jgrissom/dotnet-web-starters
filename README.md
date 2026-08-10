# .NET Web Development — Weekly Starters

**This repo is the code you copy. Nothing here is reading material, and nothing here gets edited in place.**

Each folder is one week's lab starter, ready to run:

```
week-03/
├─ FirstFlight.Web/      the lab app you build on
└─ FirstFlight.Checks/   the lab's checks — read-only
```

Weeks 1 and 2 are plain HTML, CSS and JavaScript; weeks 3 onward are ASP.NET Core MVC projects.

## Every week, three steps

**1. Pull, so you have this week's folder:**

```bash
git pull
```

**2. Copy the week's folder out** — in Finder or File Explorer, copy `week-03` (say) and paste it wherever you keep your projects, then rename the copy something meaningful. **Copy it; don't move it** — this clone keeps its own copy.

**3. Work on your copy**, from the folder holding both projects:

```bash
dotnet test FirstFlight.Checks
```

> [!CAUTION]
> **Never open or edit anything inside this clone.** It is a delivery box. If you write code in here it isn't backed up, and the next `git pull` may overwrite it or refuse to run because your changes are in the way.

## The lab is not your semester project

Two different things live on your machine, and it is worth keeping them straight:

| | |
|---|---|
| **The lab** — what you copy out of here | Never collected. **Worth zero points.** It is the guided version of that week's homework, and doing it is what turns the homework into about an hour |
| **Your own project** — your repo, your topic | **This is what gets graded.** You start it in week 4 with `dotnet new mvc` and extend it every week after |

You never copy anything from here into your project. The lab teaches the move; you then make the same move in your own app.

## Where everything else lives

**The course itself — labs, homework, lecture notes, slides — is in [dotnet-web-dev](https://github.com/jgrissom/dotnet-web-dev), and you read it in your browser.** There is nothing to clone there and nothing to keep in sync: a page you open is always the current one. Slides are published at **https://jgrissom.github.io/dotnet-web-dev/**.

You submit through Canvas — from week 3 on, that is your deployed app's URL and your project repo's URL, on two lines.
