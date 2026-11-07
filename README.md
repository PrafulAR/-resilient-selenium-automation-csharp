# Resilient Selenium Automation using C#

A C# and Selenium WebDriver automation solution that identifies webpage elements using their visible labels and accessibility information instead of depending on fixed HTML attributes such as IDs, names, and CSS classes.

## Overview

This project automates three fields on the CloudQA Automation Practice Form:

- First Name – identifies the text field and enters a value.
- Gender – identifies and selects the requested radio option.
- State – identifies the dropdown and selects the requested option.

The solution is designed to handle common webpage changes, including reordered fields, modified IDs and names, changed CSS classes, different HTML structures, and dynamically inserted elements.

## Technology Stack

- C#
- .NET 8
- Selenium WebDriver
- Selenium Support
- Google Chrome

## Project Structure

```text
resilient-selenium-automation-csharp/
│
├── Program.cs
├── SemanticLocator.cs
├── CloudQaResilientAutomation.csproj
├── README.md
└── .gitignore
```

## How It Works

### 1. Semantic Element Identification

The `SemanticLocator` class identifies elements using user-facing information rather than relying on fixed IDs, names, or CSS classes.

It considers information such as:

- Visible labels
- Associated HTML labels
- `aria-label`
- `aria-labelledby`
- Placeholder text
- Element roles and accessible information

### 2. First Name Identification

The automation searches for a text field associated with the exact label "First Name".

It avoids selecting similarly named fields such as "Preferred First Name" by using exact semantic matching.

### 3. Gender Selection

The automation identifies the Gender group and locates the requested radio option, such as Male or Female.

It verifies that the requested option is selected.

### 4. State Dropdown Selection

The solution supports:

- Native HTML `<select>` dropdowns.
- Custom dropdowns using accessible option roles.

It searches for the requested option and reports an error if that option is unavailable.

### 5. Dynamic Elements and Retry Handling

The locator uses an explicit waiting mechanism to handle elements that appear after the page loads.

A retry helper also reacquires elements when Selenium encounters a stale element reference during an interaction.

## Setup Instructions

### Prerequisites

- .NET 8 SDK
- Google Chrome
- A working internet connection for the original CloudQA website

### Install Dependencies

Open the project directory in PowerShell:

```powershell
cd D:\task1-resilient-selenium-csharp
```

Restore the NuGet packages:

```powershell
dotnet restore
```

### Build the Project

```powershell
dotnet build
```

## Running the Automation

The default target URL is:

```text
http://app.cloudqa.io/home/AutomationPracticeForm
```

Run the project:

```powershell
dotnet run
```

### Configuring Input Values

The target URL and field values can be changed using environment variables.

PowerShell example:

```powershell
$env:TARGET_URL="http://app.cloudqa.io/home/AutomationPracticeForm"
$env:FIRST_NAME="Praful"
$env:GENDER="Male"
$env:STATE_VALUE="India"

dotnet run
```

You can also provide the target URL as a command-line argument:

```powershell
dotnet run -- "http://example.com/form"
```

### Headless Execution

To run Chrome without displaying its browser window:

```powershell
$env:HEADLESS="true"
dotnet run
```

## Testing and Validation

The solution was tested against:

| Test scenario | Result |
|---|---|
| Original CloudQA practice form | Passed |
| Reordered form fields | Passed |
| Modified IDs, names and CSS classes | Passed |
| Similar field names | Passed |
| ARIA-based field identification | Passed |
| Gender radio selection | Passed |
| State dropdown selection on modified form | Passed |
| Dynamically inserted First Name field | Passed in local test |

The modified webpage tests used a local HTML test page to simulate changes in webpage structure.

## Known Limitations

- Semantic matching depends on meaningful labels or accessibility information being present in the webpage.
- Ambiguous or missing labels can make element identification difficult.
- Custom dropdown implementations vary, so additional handling may be required for some websites.
- The original CloudQA page labels a dropdown as State but provides country options. The value `India` was used to verify dropdown interaction on that page; it is not a state value.
- The solution uses rule-based semantic matching and does not depend on an AI model.

## Design Approach

The main objective is to make browser automation less dependent on implementation-specific HTML attributes.

Instead of assuming that a field always has a particular ID or CSS class, the solution looks for information that a user or assistive technology would use to understand the field.

This makes the automation more adaptable to common frontend changes while still reporting failures when the required field cannot be identified.

## Author

Developed as part of a browser automation internship assignment using C#, .NET and Selenium WebDriver.