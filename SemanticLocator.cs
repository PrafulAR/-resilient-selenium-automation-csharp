
using OpenQA.Selenium;

public sealed class SemanticLocator
{
    private readonly IWebDriver driver;

    public SemanticLocator(IWebDriver driver)
    {
        this.driver = driver;
    }

    public IWebElement WaitFor(Func<IWebElement?> finder, string description)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var element = finder();

                if (element != null && element.Displayed && element.Enabled)
                {
                    return element;
                }
            }
            catch (StaleElementReferenceException)
            {
                // DOM changed; retry the semantic search.
            }
            catch (NoSuchElementException)
            {
                // Not available yet; retry.
            }

            Thread.Sleep(250);
        }

        throw new WebDriverTimeoutException(
            $"Could not semantically locate {description} within 15 seconds.");
    }

    public IWebElement? FindTextField(string label)
    {
        return Execute(
            """
            const target = arguments[0];

            function normalize(value) {
                return String(value || "").replace(/\s+/g, " ").trim().toLowerCase();
            }

            function escapeRegex(value) {
                return value.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
            }

            function visible(el) {
                const r = el.getBoundingClientRect();
                const s = getComputedStyle(el);
                return r.width > 0 && r.height > 0 &&
                       s.display !== "none" && s.visibility !== "hidden";
            }

            function labelTexts(el) {
                const result = [];

                if (el.labels) {
                    for (const label of el.labels) {
                        result.push(label.innerText);
                    }
                }

                for (const label of document.querySelectorAll("label")) {
                    if (label.control === el || label.contains(el)) {
                        result.push(label.innerText);
                    }
                }

                const labelledBy = el.getAttribute("aria-labelledby");
                if (labelledBy) {
                    for (const id of labelledBy.split(/\s+/)) {
                        const node = document.getElementById(id);
                        if (node) result.push(node.innerText);
                    }
                }

                for (const attribute of ["aria-label", "placeholder", "title"]) {
                    const value = el.getAttribute(attribute);
                    if (value) result.push(value);
                }

                // Weak signals only: useful when a visual label is not formally connected.
                let parent = el.parentElement;
                for (let i = 0; i < 2 && parent; i++, parent = parent.parentElement) {
                    const directText = [...parent.childNodes]
                        .filter(node => node.nodeType === Node.TEXT_NODE)
                        .map(node => node.textContent || "")
                        .join(" ");

                    if (directText.trim()) {
                        result.push(directText);
                    }
                }

                return result;
            }

            function textScore(text, target) {
                const value = normalize(text);
                const wanted = normalize(target);

                if (!value || !wanted) return 0;

                // Exact label match.
                if (value === wanted) return 130;

                // Reject labels containing additional descriptive words.
                // Example: "Preferred First Name" must not match "First Name".
                return 0;
            }

            const candidates = [...document.querySelectorAll("input, textarea")]
                .filter(visible)
                .filter(el => {
                    const type = normalize(el.getAttribute("type"));
                    return ![
                        "hidden", "radio", "checkbox", "file",
                        "submit", "reset", "button", "image", "password"
                    ].includes(type);
                });

            let best = null;
            let bestScore = 0;

            for (const element of candidates) {
                let score = 0;

                for (const text of labelTexts(element)) {
                    score = Math.max(score, textScore(text, target));
                }

                const type = normalize(element.getAttribute("type"));
                if (["text", "", "email", "tel", "search"].includes(type)) {
                    score += 5;
                }

                if (score > bestScore) {
                    bestScore = score;
                    best = element;
                }
            }

            return bestScore >= 70 ? best : null;
            """,
            label);
    }

    
public IWebElement? FindRadioOption(string groupLabel, string optionLabel)
{
    return Execute(
        """
        const groupTarget = arguments[0];
        const optionTarget = arguments[1];

        function normalize(value) {
            return String(value || "")
                .replace(/\s+/g, " ")
                .trim()
                .toLowerCase();
        }

        function addText(result, value) {
            const text = String(value || "").trim();
            if (text) result.push(text);
        }

        function visible(el) {
            const r = el.getBoundingClientRect();
            const s = getComputedStyle(el);

            return r.width > 0 && r.height > 0 &&
                   s.display !== "none" &&
                   s.visibility !== "hidden";
        }

        function escapeRegex(value) {
            return value.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
        }

        function scoreText(text, target) {
            const value = normalize(text);
            const wanted = normalize(target);

            if (!value || !wanted) return 0;
            if (value === wanted) return 120;

            const exactPhrase = new RegExp(
                "(^|\\s)" + escapeRegex(wanted) + "(\\s|$)", "i"
            );

            if (exactPhrase.test(value)) return 95;
            if (value.includes(wanted)) return 70;

            const wantedTokens = wanted.split(/\s+/);
            const valueTokens = new Set(value.split(/\s+/));

            return wantedTokens.filter(t => valueTokens.has(t)).length * 10;
        }

        // Find the text describing an individual radio option.
        function labelTexts(el) {
            const result = [];

            // Associated HTML labels.
            if (el.labels) {
                for (const label of el.labels) {
                    addText(result, label.innerText);
                }
            }

            // A radio button wrapped inside a label.
            const wrappingLabel = el.closest("label");

            if (wrappingLabel) {
                addText(result, wrappingLabel.innerText);
            }

            // Accessibility attributes and the radio value.
            for (const attribute of [
                "aria-label", "title", "value"
            ]) {
                addText(result, el.getAttribute(attribute));
            }

            addText(result, el.value);

            // Text in immediately adjacent elements.
            for (const sibling of [
                el.previousElementSibling,
                el.nextElementSibling
            ]) {
                if (
                    sibling &&
                    !sibling.matches("input[type='radio'], [role='radio']")
                ) {
                    addText(result, sibling.innerText);
                }
            }

            // Text nodes immediately before or after the input.
            for (
                let node = el.previousSibling;
                node && node.nodeType === Node.TEXT_NODE;
                node = node.previousSibling
            ) {
                addText(result, node.textContent);
            }

            for (
                let node = el.nextSibling;
                node && node.nodeType === Node.TEXT_NODE;
                node = node.nextSibling
            ) {
                addText(result, node.textContent);
            }

            // Inspect nearby sibling nodes without taking the entire
            // radio group's text as the individual option label.
            const parent = el.parentElement;

            if (parent) {
                const children = [...parent.childNodes];
                const index = children.indexOf(el);

                for (const direction of [-1, 1]) {
                    for (let step = 1; step <= 2; step++) {
                        const node = children[index + direction * step];

                        if (!node) break;

                        if (
                            node.nodeType === Node.ELEMENT_NODE &&
                            node.matches(
                                "input[type='radio'], [role='radio']"
                            )
                        ) {
                            break;
                        }

                        if (node.nodeType === Node.TEXT_NODE) {
                            addText(result, node.textContent);
                        } else if (node.nodeType === Node.ELEMENT_NODE) {
                            if (
                                node.querySelector(
                                    "input[type='radio'], [role='radio']"
                                )
                            ) {
                                break;
                            }

                            addText(result, node.innerText);
                        }
                    }
                }
            }

            return result;
        }

        // Find the surrounding Gender group.
        function groupTexts(el) {
            const result = [];
            let node = el.parentElement;

            for (let i = 0; i < 6 && node; i++, node = node.parentElement) {
                if (
                    node.matches(
                        "fieldset, [role='group'], [role='radiogroup']"
                    )
                ) {
                    addText(result, node.innerText);
                }

                const radioCount = node.querySelectorAll(
                    "input[type='radio'], [role='radio']"
                ).length;

                if (radioCount >= 2 && radioCount <= 10) {
                    addText(result, node.innerText);
                }
            }

            return result;
        }

        const candidates = [
            ...document.querySelectorAll(
                "input[type='radio'], [role='radio']"
            )
        ].filter(el => visible(el) && !el.disabled);

        let best = null;
        let bestScore = 0;

        for (const element of candidates) {
            let groupScore = 0;
            let optionScore = 0;

            for (const text of groupTexts(element)) {
                groupScore = Math.max(
                    groupScore,
                    scoreText(text, groupTarget)
                );
            }

            for (const text of labelTexts(element)) {
                optionScore = Math.max(
                    optionScore,
                    scoreText(text, optionTarget)
                );
            }

            // Require evidence for both the group and the option.
            if (groupScore < 70 || optionScore < 70) {
                continue;
            }

            const total = groupScore + optionScore;

            if (total > bestScore) {
                bestScore = total;
                best = element;
            }
        }

        return best;
        """,
        groupLabel,
        optionLabel);
}

    
public IWebElement? FindDropdown(string label)
{
    return Execute(
        """
        const target = String(arguments[0] || "")
            .replace(/\s+/g, " ")
            .trim()
            .toLowerCase();

        function normalize(value) {
            return String(value || "")
                .replace(/\s+/g, " ")
                .trim()
                .toLowerCase();
        }

        function visible(el) {
            const rect = el.getBoundingClientRect();
            const style = getComputedStyle(el);

            return rect.width > 0 &&
                   rect.height > 0 &&
                   style.display !== "none" &&
                   style.visibility !== "hidden" &&
                   !el.disabled;
        }

        function scoreText(text) {
            const value = normalize(text);

            if (!value || !target) return 0;

            if (value === target) return 150;

            const escaped = target.replace(
                /[.*+?^${}()|[\]\\]/g, "\\$&"
            );

            const exactPhrase = new RegExp(
                "(^|\\s)" + escaped + "(\\s|$)", "i"
            );

            if (exactPhrase.test(value)) return 120;

            if (value.includes(target)) return 80;

            return 0;
        }

        // Collect only meaningful semantic relationships.
        function associatedTexts(el) {
            const result = [];

            // 1. Native HTML label association.
            if (el.labels) {
                for (const label of el.labels) {
                    result.push(label.innerText);
                }
            }

            // 2. Labels explicitly referencing the element.
            for (const label of document.querySelectorAll("label")) {
                if (label.control === el || label.contains(el)) {
                    result.push(label.innerText);
                }
            }

            // 3. Accessibility relationships.
            const labelledBy = el.getAttribute("aria-labelledby");

            if (labelledBy) {
                for (const id of labelledBy.split(/\s+/)) {
                    const node = document.getElementById(id);

                    if (node) {
                        result.push(node.innerText);
                    }
                }
            }

            // 4. Direct accessibility attributes.
            for (const attribute of [
                "aria-label",
                "title",
                "placeholder"
            ]) {
                const value = el.getAttribute(attribute);

                if (value) {
                    result.push(value);
                }
            }

            // 5. A wrapping label.
            const wrappingLabel = el.closest("label");

            if (wrappingLabel) {
                result.push(wrappingLabel.innerText);
            }

            // 6. Check nearby preceding label-like elements.
            const previous = el.previousElementSibling;

            if (
                previous &&
                ["LABEL", "SPAN"].includes(previous.tagName)
            ) {
                result.push(previous.innerText);
            }

            return result;
        }

        const candidates = [
            ...document.querySelectorAll(
                "select, [role='combobox'], input[aria-haspopup='listbox']"
            )
        ].filter(visible);

        const scored = candidates.map(element => {
            const texts = associatedTexts(element);

            const scores = texts.map(scoreText);
            const bestScore = Math.max(0, ...scores);

            return {
                element,
                score: bestScore
            };
        });

        scored.sort((a, b) => b.score - a.score);

        if (scored.length === 0) {
            return null;
        }

        const best = scored[0];
        const second = scored[1];

        // Require a strong semantic match.
        if (best.score < 80) {
            return null;
        }

        // Do not guess when multiple elements have equal scores.
        if (second && second.score === best.score) {
            return null;
        }

        return best.element;
        """,
        label
    );
}


    private IWebElement? Execute(string script, params object[] arguments)
    {
        return ((IJavaScriptExecutor)driver).ExecuteScript(script, arguments)
            as IWebElement;
    }
}
