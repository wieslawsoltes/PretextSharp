namespace PretextSamples.Samples;

public sealed record AccordionItem(string Title, string Text);

public static class AccordionSampleData
{
    public static readonly IReadOnlyList<AccordionItem> Items =
    [
        new("Section 1", "Mina cut the release note to three crisp lines, then realized the support caveat still needed one more sentence before it could ship without surprises."),
        new("Section 2", "The handoff doc now reads like a proper morning checklist instead of a diary entry. Restart the worker, verify the queue drains, and only then mark the incident quiet. If the backlog grows again, page the same owner instead of opening a new thread."),
        new("Section 3", "We learned the hard way that a giant native scroll range can dominate everything else. The bug looked like DOM churn, then like pooling, then like rendering pressure, until the repros were stripped down enough to show the real limit. That changed the fix completely: simplify the DOM, keep virtualization honest, and stop hiding the worst-case path behind caches that only make the common frame look cheaper."),
        new("Section 4", "AGI 春天到了. بدأت الرحلة 🚀 and the long URL is https://example.com/reports/q3?lang=ar&mode=full. Nora wrote “please keep 10 000 rows visible,” Mina replied “trans­atlantic labels are still weird.”"),
    ];
}
