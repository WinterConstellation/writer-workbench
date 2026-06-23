namespace WriterWorkbench.Core.Help;

public sealed record HelpTopic(
    string Section,
    string Item,
    string Role,
    string Usage
);
