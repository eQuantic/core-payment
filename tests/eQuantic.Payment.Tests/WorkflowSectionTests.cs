namespace eQuantic.Payment.Tests;

/// <summary>
/// The working agreement is the <c>## Workflow</c> section of <c>CLAUDE.md</c>, which Claude Code
/// reads, written again in <c>AGENTS.md</c> for the agents that read that file instead. Two copies
/// kept by hand drift apart, so this compares them line by line.
/// </summary>
public class WorkflowSectionTests
{
    private const string Heading = "## Workflow";

    [Fact]
    public void Claude_md_and_agents_md_carry_the_same_workflow_section()
    {
        var root = RepositoryRoot();
        var claude = Section(root, "CLAUDE.md");
        var agents = Section(root, "AGENTS.md");

        for (var i = 0; i < Math.Max(claude.Lines.Count, agents.Lines.Count); i++)
        {
            var left = i < claude.Lines.Count ? claude.Lines[i] : null;
            var right = i < agents.Lines.Count ? agents.Lines[i] : null;
            if (left == right)
                continue;

            Assert.Fail(
                $"The Workflow sections differ first at CLAUDE.md:{claude.FirstLine + i} and AGENTS.md:{agents.FirstLine + i}\n" +
                $"  CLAUDE.md: {left ?? "(the section has ended)"}\n" +
                $"  AGENTS.md: {right ?? "(the section has ended)"}");
        }
    }

    private sealed record WorkflowSection(int FirstLine, IReadOnlyList<string> Lines);

    // From the heading to the next heading of level one or two outside a code fence, or to the end.
    private static WorkflowSection Section(string root, string file)
    {
        var path = Path.Combine(root, file);
        if (!File.Exists(path))
            Assert.Fail($"{file} is not at the repository root ({root}), so it has no Workflow section to compare");

        var lines = File.ReadAllLines(path);
        var start = Array.FindIndex(lines, line => line.TrimEnd() == Heading);
        if (start < 0)
            Assert.Fail($"{file} has no '{Heading}' section to compare");

        var section = new List<string> { lines[start] };
        var fenced = false;
        for (var i = start + 1; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line.TrimStart().StartsWith("```", StringComparison.Ordinal))
                fenced = !fenced;
            else if (!fenced && (line.StartsWith("# ", StringComparison.Ordinal) || line.StartsWith("## ", StringComparison.Ordinal)))
                break;
            section.Add(line);
        }

        return new WorkflowSection(start + 1, section);
    }

    private static string RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "eQuantic.Payment.slnx")))
                return directory.FullName;
        }

        throw new DirectoryNotFoundException(
            $"No eQuantic.Payment.slnx above {AppContext.BaseDirectory}, so the repository's CLAUDE.md and AGENTS.md cannot be found");
    }
}
