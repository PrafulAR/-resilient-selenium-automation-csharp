
using System;
using System.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

// ==========================================
// 1. CONFIGURATION
// ==========================================

var targetUrl =
    args.FirstOrDefault()
    ?? Environment.GetEnvironmentVariable("TARGET_URL")
    ?? "http://app.cloudqa.io/home/AutomationPracticeForm";

var firstNameValue =
    Environment.GetEnvironmentVariable("FIRST_NAME")
    ?? "Praful";

var genderValue =
    Environment.GetEnvironmentVariable("GENDER")
    ?? "Male";

var stateValue =
    Environment.GetEnvironmentVariable("STATE_VALUE")
    ?? "Karnataka";

var headless = string.Equals(
    Environment.GetEnvironmentVariable("HEADLESS"),
    "true",
    StringComparison.OrdinalIgnoreCase
);

// ==========================================
// 2. BROWSER INITIALIZATION
// ==========================================

var options = new ChromeOptions();
options.AddArgument("--window-size=1440,1000");

if (headless)
{
    options.AddArgument("--headless=new");
}

using var driver = new ChromeDriver(options);

try
{
    driver.Navigate().GoToUrl(targetUrl);

    var finder = new SemanticLocator(driver);

    Console.WriteLine($"Target URL: {targetUrl}");
    Console.WriteLine("Locating fields by visible/user-facing meaning...");
    Console.WriteLine();

    // ==========================================
    // 3. FIRST NAME
    // ==========================================

    PerformWithRetry(
        () => finder.WaitFor(
            () => finder.FindTextField("First Name"),
            "First Name text field"
        ),
        element =>
        {
            element.Clear();
            element.SendKeys(firstNameValue);
        },
        "First Name input"
    );

    // Reacquire the element before verification.
    var firstName = finder.WaitFor(
        () => finder.FindTextField("First Name"),
        "First Name verification"
    );

    var actualFirstName = ReadValue(driver, firstName);

    if (!string.Equals(
        actualFirstName,
        firstNameValue,
        StringComparison.Ordinal))
    {
        throw new InvalidOperationException(
            $"First Name verification failed. Expected '{firstNameValue}', got '{actualFirstName}'."
        );
    }

    Console.WriteLine($"PASS: First Name = {actualFirstName}");

    // ==========================================
    // 4. GENDER
    // ==========================================

    PerformWithRetry(
        () => finder.WaitFor(
            () => finder.FindRadioOption("Gender", genderValue),
            $"Gender option '{genderValue}'"
        ),
        element =>
        {
            if (!element.Selected)
            {
                element.Click();
            }

            if (!element.Selected)
            {
                throw new InvalidOperationException(
                    $"Gender '{genderValue}' could not be selected."
                );
            }
        },
        $"Gender option '{genderValue}'"
    );

    Console.WriteLine($"PASS: Gender = {genderValue}");

    // ==========================================
    // 5. STATE DROPDOWN
    // ==========================================

    PerformWithRetry(
        () => finder.WaitFor(
            () => finder.FindDropdown("State"),
            "State dropdown"
        ),
        dropdown => SelectState(driver, dropdown, stateValue),
        "State dropdown"
    );

    Console.WriteLine($"PASS: State = {stateValue}");

    // ==========================================
    // 6. FINAL RESULT
    // ==========================================

    Console.WriteLine();
    Console.WriteLine("==========================================");
    Console.WriteLine("RESULT: PASS");
    Console.WriteLine("==========================================");

    Console.WriteLine(
        "All requested fields were successfully processed."
    );
}
catch (Exception ex)
{
    Console.Error.WriteLine();
    Console.Error.WriteLine("==========================================");
    Console.Error.WriteLine("RESULT: FAIL");
    Console.Error.WriteLine("==========================================");

    Console.Error.WriteLine(ex.ToString());

    Environment.ExitCode = 1;
}
finally
{
    driver.Quit();
}

// ==========================================
// HELPER 1: READ INPUT VALUE
// ==========================================

static string ReadValue(IWebDriver driver, IWebElement element)
{
    return ((IJavaScriptExecutor)driver).ExecuteScript(
        """
        const el = arguments[0];
        return el.value ?? el.getAttribute("aria-label") ?? el.innerText ?? "";
        """,
        element
    )?.ToString() ?? "";
}

// ==========================================
// HELPER 2: SELECT STATE
// ==========================================

static void SelectState(
    IWebDriver driver,
    IWebElement dropdown,
    string expectedState)
{
    // Handle native HTML select dropdowns.
    if (dropdown.TagName.Equals(
        "select",
        StringComparison.OrdinalIgnoreCase))
    {
        var select = new SelectElement(dropdown);

        var option = select.Options.FirstOrDefault(o =>
            Normalize(o.Text) == Normalize(expectedState)
        );

        option ??= select.Options.FirstOrDefault(o =>
            Normalize(o.Text).Contains(Normalize(expectedState))
        );

        if (option == null)
        {
            var available = string.Join(
                ", ",
                select.Options.Select(o => o.Text.Trim())
            );

            throw new InvalidOperationException(
                $"State '{expectedState}' was not found. Available options: {available}"
            );
        }

        select.SelectByText(option.Text);

        if (!Normalize(select.SelectedOption.Text)
            .Equals(Normalize(expectedState)))
        {
            throw new InvalidOperationException(
                $"State selection verification failed for '{expectedState}'."
            );
        }

        return;
    }

    // Handle custom ARIA dropdowns.
    dropdown.Click();

    var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));

    var optionElement = wait.Until(d =>
        FindVisibleOption(d, expectedState)
    );

    if (optionElement == null)
    {
        throw new InvalidOperationException(
            $"Could not find visible dropdown option '{expectedState}'."
        );
    }

    optionElement.Click();
}

// ==========================================
// HELPER 3: FIND VISIBLE DROPDOWN OPTION
// ==========================================

static IWebElement? FindVisibleOption(
    IWebDriver driver,
    string expectedText)
{
    var result = ((IJavaScriptExecutor)driver).ExecuteScript(
        """
        const expected = String(arguments[0] || "")
            .replace(/\s+/g, " ")
            .trim()
            .toLowerCase();

        function visible(el) {
            const r = el.getBoundingClientRect();
            const s = getComputedStyle(el);

            return r.width > 0 &&
                   r.height > 0 &&
                   s.display !== "none" &&
                   s.visibility !== "hidden";
        }

        function normalize(value) {
            return String(value || "")
                .replace(/\s+/g, " ")
                .trim()
                .toLowerCase();
        }

        const candidates = [
            ...document.querySelectorAll(
                '[role="option"], option, [role="listbox"] li'
            )
        ].filter(visible);

        const exact = candidates.find(
            el => normalize(el.innerText || el.textContent) === expected
        );

        if (exact) return exact;

        return candidates.find(
            el => normalize(el.innerText || el.textContent).includes(expected)
        ) || null;
        """,
        expectedText
    );

    return result as IWebElement;
}

// ==========================================
// HELPER 4: NORMALIZE TEXT
// ==========================================

static string Normalize(string value)
{
    return string.Concat(
        value.Where(c => !char.IsWhiteSpace(c))
    ).ToLowerInvariant();
}

// ==========================================
// HELPER 5: RETRY ACTION WHEN ELEMENT IS STALE
// ==========================================

static void PerformWithRetry(
    Func<IWebElement?> finder,
    Action<IWebElement> action,
    string description,
    int maxAttempts = 3)
{
    for (int attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            var element = finder();

            if (element == null)
            {
                throw new NoSuchElementException(
                    $"{description} could not be found."
                );
            }

            action(element);

            return;
        }
        catch (StaleElementReferenceException)
        {
            Console.WriteLine(
                $"Stale element detected. Retrying {description} " +
                $"(attempt {attempt}/{maxAttempts})..."
            );

            if (attempt == maxAttempts)
            {
                throw;
            }
        }
    }
}
