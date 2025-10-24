using System;
using System.Collections.Generic;
using System.Linq;

namespace FullCrisis3;

/// <summary>
/// Represents different types of player input in a story
/// </summary>
public enum InputType
{
    None,           // No input required, just continue
    TextInput,      // Free text input from player
    Choice,         // Select from 2-3 button choices
    Dropdown       // Select from dropdown menu
}

/// <summary>
/// Represents a single dialogue/scene in the story
/// </summary>
public class StoryDialogue
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Text { get; set; } = "";
    public InputType InputType { get; set; } = InputType.None;
    
    // For text input
    public string InputPrompt { get; set; } = "";
    public string InputVariableName { get; set; } = ""; // Variable name to store the input
    
    // For choices and dropdown
    public List<StoryChoice> Choices { get; set; } = new();
    
    // Next dialogue ID (used when no input or after processing input)
    public string NextDialogueId { get; set; } = "";
    
    // Conditional logic - can reference stored variables
    public Func<StoryState, string>? ConditionalNext { get; set; }
}

/// <summary>
/// Represents a choice option in the story
/// </summary>
public class StoryChoice
{
    public string Text { get; set; } = "";
    public string Value { get; set; } = ""; // Value stored in variable
    public string NextDialogueId { get; set; } = "";
    public string? VariableName { get; set; } // Variable to store this choice in
}

/// <summary>
/// Stores the current state of a story playthrough
/// </summary>
public class StoryState
{
    public string StoryId { get; set; } = "";
    public string PlayerName { get; set; } = "";
    public string CurrentDialogueId { get; set; } = "";
    public Dictionary<string, string> Variables { get; set; } = new();
    public DateTime LastPlayed { get; set; } = DateTime.UtcNow;
    
    public string GetVariable(string name, string defaultValue = "")
    {
        return Variables.TryGetValue(name, out var value) ? value : defaultValue;
    }
    
    public void SetVariable(string name, string value)
    {
        Variables[name] = value;
    }
}

/// <summary>
/// Represents a complete story with metadata
/// </summary>
public class Story
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string StartDialogueId { get; set; } = "";
    public Dictionary<string, StoryDialogue> Dialogues { get; set; } = new();
    
    public StoryDialogue? GetDialogue(string id)
    {
        return Dialogues.TryGetValue(id, out var dialogue) ? dialogue : null;
    }
}

/// <summary>
/// Story builder class - provides a DSL for creating stories
/// </summary>
public class StoryBuilder
{
    private readonly Story _story;
    private StoryDialogue? _currentDialogue;
    
    public StoryBuilder(string id, string title, string description)
    {
        _story = new Story
        {
            Id = id,
            Title = title,
            Description = description
        };
    }
    
    public StoryBuilder StartAt(string dialogueId)
    {
        _story.StartDialogueId = dialogueId;
        return this;
    }
    
    public StoryBuilder Dialogue(string id, string title, string text)
    {
        _currentDialogue = new StoryDialogue
        {
            Id = id,
            Title = title,
            Text = text
        };
        _story.Dialogues[id] = _currentDialogue;
        return this;
    }
    
    public StoryBuilder ContinueTo(string nextDialogueId)
    {
        if (_currentDialogue != null)
        {
            _currentDialogue.NextDialogueId = nextDialogueId;
        }
        return this;
    }
    
    public StoryBuilder TextInput(string prompt, string variableName)
    {
        if (_currentDialogue != null)
        {
            _currentDialogue.InputType = InputType.TextInput;
            _currentDialogue.InputPrompt = prompt;
            _currentDialogue.InputVariableName = variableName;
        }
        return this;
    }
    
    public StoryBuilder Choice(string text, string value, string nextDialogueId, string? variableName = null)
    {
        if (_currentDialogue != null)
        {
            if (_currentDialogue.InputType == InputType.None)
            {
                _currentDialogue.InputType = InputType.Choice;
            }
            
            _currentDialogue.Choices.Add(new StoryChoice
            {
                Text = text,
                Value = value,
                NextDialogueId = nextDialogueId,
                VariableName = variableName
            });
        }
        return this;
    }
    
    public StoryBuilder DropdownChoice(string text, string value, string? variableName = null)
    {
        if (_currentDialogue != null)
        {
            if (_currentDialogue.InputType == InputType.None)
            {
                _currentDialogue.InputType = InputType.Dropdown;
            }
            
            _currentDialogue.Choices.Add(new StoryChoice
            {
                Text = text,
                Value = value,
                VariableName = variableName
            });
        }
        return this;
    }
    
    public StoryBuilder ConditionalNext(Func<StoryState, string> condition)
    {
        if (_currentDialogue != null)
        {
            _currentDialogue.ConditionalNext = condition;
        }
        return this;
    }
    
    public Story Build()
    {
        return _story;
    }
}

/// <summary>
/// Main story engine that manages story execution
/// </summary>
public class StoryEngine
{
    private readonly Dictionary<string, Story> _stories = new();
    
    public void RegisterStory(Story story)
    {
        _stories[story.Id] = story;
    }
    
    public IEnumerable<Story> GetAvailableStories()
    {
        return _stories.Values;
    }
    
    public Story? GetStory(string id)
    {
        return _stories.TryGetValue(id, out var story) ? story : null;
    }
    
    public StoryState StartNewGame(string storyId, string playerName)
    {
        var story = GetStory(storyId);
        if (story == null)
            throw new ArgumentException($"Story '{storyId}' not found");
            
        return new StoryState
        {
            StoryId = storyId,
            PlayerName = playerName,
            CurrentDialogueId = story.StartDialogueId,
            LastPlayed = DateTime.UtcNow
        };
    }
    
    public StoryDialogue? GetCurrentDialogue(StoryState state)
    {
        var story = GetStory(state.StoryId);
        return story?.GetDialogue(state.CurrentDialogueId);
    }
    
    public string ProcessDialogueText(StoryDialogue dialogue, StoryState state)
    {
        var text = dialogue.Text;
        
        // Replace variable placeholders like {playerName}, {hometown}, etc.
        text = text.Replace("{playerName}", state.PlayerName);
        
        foreach (var variable in state.Variables)
        {
            text = text.Replace($"{{{variable.Key}}}", variable.Value);
        }
        
        return text;
    }
    
    public void ProcessPlayerInput(StoryState state, string input, StoryChoice? selectedChoice = null)
    {
        var dialogue = GetCurrentDialogue(state);
        if (dialogue == null) return;
        
        switch (dialogue.InputType)
        {
            case InputType.TextInput:
                if (!string.IsNullOrEmpty(dialogue.InputVariableName))
                {
                    state.SetVariable(dialogue.InputVariableName, input);
                }
                break;
                
            case InputType.Choice:
            case InputType.Dropdown:
                if (selectedChoice?.VariableName != null)
                {
                    state.SetVariable(selectedChoice.VariableName, selectedChoice.Value);
                }
                break;
        }
        
        // Determine next dialogue
        string nextDialogueId = "";
        
        if (selectedChoice != null && !string.IsNullOrEmpty(selectedChoice.NextDialogueId))
        {
            nextDialogueId = selectedChoice.NextDialogueId;
        }
        else if (dialogue.ConditionalNext != null)
        {
            nextDialogueId = dialogue.ConditionalNext(state);
        }
        else
        {
            nextDialogueId = dialogue.NextDialogueId;
        }
        
        state.CurrentDialogueId = nextDialogueId;
        state.LastPlayed = DateTime.UtcNow;
    }
    
    public bool IsStoryComplete(StoryState state)
    {
        return string.IsNullOrEmpty(state.CurrentDialogueId) || GetCurrentDialogue(state) == null;
    }
}