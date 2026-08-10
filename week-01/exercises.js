// Week 1 Lab — JS Refresher Gauntlet
// Complete each TODO. Refresh index.html in the browser to re-run the checks.
// Rules: const/let only (no var). === only (no ==).

// ── Exercise 1: describeScore ────────────────────────────────
// Return the string:  NAME scored SCORE points
// Example: describeScore("Ada", 90) → "Ada scored 90 points"
// Use a template literal (backticks), not + concatenation.
const describeScore = (name, score) => {
  // TODO
};

// ── Exercise 2: isPassing ────────────────────────────────────
// Return true if score is 70 or higher, false otherwise.
const isPassing = (score) => {
  // TODO
};

// ── Exercise 3: curve ────────────────────────────────────────
// Given an array of scores, return a NEW array with 5 added to each.
// Example: curve([90, 60]) → [95, 65]
// Use .map()
const curve = (scores) => {
  // TODO
};

// ── Exercise 4: passingScores ────────────────────────────────
// Given an array of scores, return a NEW array containing only
// the scores that are 70 or higher. Use .filter()
// Hint: you already wrote isPassing — reuse it!
const passingScores = (scores) => {
  // TODO
};

// ── Exercise 5: findStudent ──────────────────────────────────
// Given an array of student objects and a name, return the student
// object with that name (or undefined if not found). Use .find()
// Example: findStudent([{ name: "Ada", gpa: 3.9 }], "Ada") → { name: "Ada", gpa: 3.9 }
const findStudent = (students, name) => {
  // TODO
};

// ── Exercise 6: honorRollNames ───────────────────────────────
// Given an array of student objects, return an array of the NAMES
// of students with a gpa of 3.5 or higher.
// Chain .filter() and .map()
// Example: [{ name: "Ada", gpa: 3.9 }, { name: "Linus", gpa: 3.4 }] → ["Ada"]
const honorRollNames = (students) => {
  // TODO
};

// ── Exercise 7: formatStudent ────────────────────────────────
// Given a student object like { name: "Ada", gpa: 3.9 },
// return the string:  Ada (GPA: 3.9)
// Use destructuring to pull out name and gpa first.
const formatStudent = (student) => {
  // TODO
};

// ── Exercise 8: addStudent ───────────────────────────────────
// Given an array of students and a new student object, return a
// NEW array with the student added at the end.
// Do NOT mutate the original array — no .push()! Use spread: [... ]
const addStudent = (students, newStudent) => {
  // TODO
};
