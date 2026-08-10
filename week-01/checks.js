// Week 1 Lab — automated checks. Students: read, but don't edit!
(() => {
  const results = [];
  const check = (num, label, fn) => {
    try {
      const pass = fn();
      results.push({ num, label, pass: pass === true });
    } catch (e) {
      results.push({ num, label, pass: false, err: e.message });
    }
  };

  const students = [
    { name: "Ada", gpa: 3.9 },
    { name: "Linus", gpa: 3.4 },
    { name: "Grace", gpa: 4.0 },
  ];

  check(1, "describeScore", () => describeScore("Ada", 90) === "Ada scored 90 points");
  check(2, "isPassing", () => isPassing(70) === true && isPassing(69) === false);
  check(3, "curve", () => JSON.stringify(curve([90, 60])) === "[95,65]");
  check(4, "passingScores", () => JSON.stringify(passingScores([90, 60, 70])) === "[90,70]");
  check(5, "findStudent", () => {
    const s = findStudent(students, "Grace");
    return s !== undefined && s.gpa === 4.0 && findStudent(students, "Nobody") === undefined;
  });
  check(6, "honorRollNames", () => JSON.stringify(honorRollNames(students)) === '["Ada","Grace"]');
  check(7, "formatStudent", () => formatStudent({ name: "Ada", gpa: 3.9 }) === "Ada (GPA: 3.9)");
  check(8, "addStudent (no mutation!)", () => {
    const original = [{ name: "Ada", gpa: 3.9 }];
    const added = addStudent(original, { name: "Grace", gpa: 4.0 });
    return added.length === 2 && added[1].name === "Grace" && original.length === 1;
  });

  console.log("%c── Week 1 Lab Results ──", "font-weight: bold");
  for (const r of results) {
    const icon = r.pass ? "✅" : "❌";
    const extra = r.err ? `  (error: ${r.err})` : "";
    console.log(`${icon} Exercise ${r.num}: ${r.label}${extra}`);
  }
  const passed = results.filter(r => r.pass).length;
  console.log(`%c${passed} / ${results.length} passing`, passed === results.length ? "color: green; font-weight: bold" : "color: orange; font-weight: bold");
})();
