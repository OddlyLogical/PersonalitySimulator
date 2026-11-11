using System.Text.RegularExpressions;

namespace PersonalitySimulator.Services;

public sealed class ConversationTone
{
    public bool Serious { get; private set; }
    public bool PersonalTeasingAllowed { get; private set; } = true;
    private const string RequestStart = @"(?:^|[.!?;]\s*)(?:(?:ok(?:ay)?|yes|sure)[, ]+)?(?:please )?(?:(?:can|could|would) you (?:please )?)?";
    private static readonly Regex PersonalPermission = new(
        RequestStart + @"(?:roast me|give me a playful roast|you can tease me again|tease me(?: again)?)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
    private static readonly Regex QuestionPermission = new(
        RequestStart + @"ask me (?:a |one |another |some |two |three )?questions?\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
    private static readonly Regex PersonalRefusal = new(
        @"\b(?:can't|cannot|can not|may not|must not|should not|not allowed to|don't want (?:you )?to|do not want (?:you )?to|(?:didn't|did not) (?:ask|tell|invite) you to) (?:roast|tease|mock|make fun of|joke about) me\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
    private static readonly Regex PersonalBoundary = new(
        @"\b((?:don't|do not|never|stop) (?:making fun of|mocking|teasing|roasting|joking about) me|(?:don't|do not|never) (?:mock|tease|roast|joke about|make fun of) me|jokes?.{0,100}(?:rather than|instead of|not) (?:about )?me)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
    private static readonly Regex Stop = new(
        @"\b(stop (?:teasing|the jokes|joking)|stop it|that's enough|that is enough|no jokes|(?<!don't )(?<!do not )(?<!never )be serious|serious (?:answer|mode)|(?:don't|do not|never) (?:roast|tease|joke)|that joke hurt|that was too sharp)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
    private static readonly Regex Resume = new(
        @"\b(let(?:'?s| us) joke again|playful mode|(?:don't|do not|never) be (?:so |too )?serious)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    public void Observe(string input)
    {
        var text = ConversationText.Normalize(input);
        var explicitRefusal = PersonalRefusal.IsMatch(text);
        var refusesPersonal = PersonalBoundary.IsMatch(text) || explicitRefusal;
        var invitesPersonal = PersonalPermission.IsMatch(text);
        if (refusesPersonal) PersonalTeasingAllowed = false;
        if (text.Trim().TrimEnd('.', '!').Equals("stop", StringComparison.OrdinalIgnoreCase) || Stop.IsMatch(text) || explicitRefusal) Serious = true;
        else if (Resume.IsMatch(text) || invitesPersonal)
        {
            Serious = false;
            // Resuming jokes about a topic is not permission to resume personal teasing.
            if (!refusesPersonal && invitesPersonal)
                PersonalTeasingAllowed = true;
        }
    }

    public string Guidance(string input)
    {
        var normalized = ConversationText.Normalize(input);
        var questionRequested = QuestionPermission.IsMatch(normalized);
        var tone = Serious
            ? "Current tone: SERIOUS. No jokes, teasing, sarcasm, or motivational praise. Give a plain, useful answer."
            : "Current tone: PLAYFUL. Humor is optional, not a task to perform on every turn. First fulfill the user's actual request; use a dry aside only when it fits. For practical questions, give a useful answer.";
        return "\nCurrent response instructions: " + tone
            + (PersonalTeasingAllowed ? "" : " Personal teasing is OFF. Humor may target the topic or situation, never the user, their competence, or their feelings. A new topic or a request to joke again does not remove this boundary.")
            + " Respond to the latest actual user message, not an earlier topic. Do not recycle an old joke for a different topic."
            + " Stay engaged when the user wants conversation. Do not shoo them away, tell them to go do something else, or dismiss feedback with another joke. Acknowledge conversational feedback plainly and adjust your next contribution."
            + " Acknowledgments, small corrections, and factual recaps need no punchline. Accept a correction briefly and plainly; do not retell your previous joke with the corrected detail substituted."
            + " In fiction and hypothetical scenarios, preserve the user's explicit premises and corrections. Your earlier invented details are proposals, not facts that override the user. When corrected, discard the contradictory detail and repair the causal explanation. Explain how the stated cause produces the stated effect; do not replace a requested real danger with an imagined danger, dream, misunderstanding, or twist. Before answering, check that the answer satisfies every explicit constraint in the latest request."
            + " A rejected topic is a boundary, not material for another joke. When asked to change topics or pick something else, actually introduce a specific different subject with something interesting to say about it. Do not mention the rejected subject again, even sarcastically. Saved interests do not override this request."
            + (questionRequested ? " The user requested a question: ask at most one."
                : " End with a statement. Do not ask a follow-up question or offer a customer-service interview.");
    }

    public static bool QuestionRequested(string input) => QuestionPermission.IsMatch(ConversationText.Normalize(input));
}
