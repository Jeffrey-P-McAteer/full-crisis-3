using System;
using System.Collections.Generic;

namespace FullCrisis3;

/// <summary>
/// Contains example stories to demonstrate the story engine
/// </summary>
public static class ExampleStories
{
    public static Story CreateFirefighterStory()
    {
        return new StoryBuilder("firefighter", "Firefighter Crisis", "A blazing apartment building tests your emergency response skills.")
            .StartAt("intro")
            
            .Dialogue("intro", "Welcome, Firefighter", 
                "Welcome to the Fire Department, {playerName}! You've just been called to a 4-story apartment building that's on fire. " +
                "People are trapped inside and time is running out. As the incident commander, every decision you make could save or cost lives.")
            .TextInput("What is your hometown? (This will affect your experience level)", "hometown")
            .ContinueTo("experience_check")
            
            .Dialogue("experience_check", "Your Background", 
                "Being from {hometown} gives you some unique perspective. Now, what's your primary concern arriving at the scene?")
            .Choice("Assess the building structure first", "assess", "structure_choice", "priority")
            .Choice("Immediately start evacuating civilians", "evacuate", "evacuation_choice", "priority")
            .Choice("Set up water supply lines", "water", "water_choice", "priority")
            
            .Dialogue("structure_choice", "Structural Assessment", 
                "Good thinking! You notice the building's {hometown} construction style. The fire is concentrated on the 2nd floor. " +
                "What's your evacuation strategy?")
            .DropdownChoice("Ladder rescue from windows", "ladder")
            .DropdownChoice("Interior stairwell evacuation", "stairwell", "evacuation_method")
            .DropdownChoice("Aerial platform rescue", "aerial")
            .ConditionalNext(state => "final_outcome")
            
            .Dialogue("evacuation_choice", "Direct Action", 
                "You rush toward the building entrance. Your {hometown} training kicks in as you see smoke pouring from the windows. " +
                "Several residents are at the 3rd floor windows. How do you coordinate the rescue?")
            .DropdownChoice("Ground ladder teams to each window", "ground_ladders")
            .DropdownChoice("Aerial ladder to the roof", "aerial_roof", "evacuation_method")
            .DropdownChoice("Interior search and rescue", "interior_search")
            .ConditionalNext(state => "final_outcome")
            
            .Dialogue("water_choice", "Water Supply", 
                "Smart! Establishing water supply is crucial. Your experience from {hometown} helps you locate the hydrants quickly. " +
                "The building is now fully engulfed. What's your water strategy?")
            .DropdownChoice("Defensive exterior attack", "defensive")
            .DropdownChoice("Interior suppression attack", "interior", "evacuation_method")
            .DropdownChoice("Aerial water tower", "aerial_water")
            .ConditionalNext(state => "final_outcome")
            
            .Dialogue("final_outcome", "Mission Complete", 
                "Excellent work, {playerName}! Your decision to prioritize {priority} and use {evacuation_method} tactics " +
                "proved effective. Thanks to your {hometown} background and quick thinking, all civilians were safely evacuated. " +
                "The building suffered damage, but no lives were lost. Well done, Chief!")
            
            .Build();
    }
    
    public static Story CreateMedicalStory()
    {
        return new StoryBuilder("medical", "Medical Emergency", "A mass casualty incident at a local festival requires your medical expertise.")
            .StartAt("intro")
            
            .Dialogue("intro", "Emergency Medical Response", 
                "Dr. {playerName}, you're the lead medic responding to a mass casualty incident at the annual music festival. " +
                "A stage collapsed, injuring multiple people. You need to establish triage and coordinate medical response.")
            .TextInput("What is your medical specialty? (e.g., Emergency Medicine, Surgery, Trauma)", "specialty")
            .ContinueTo("scene_assessment")
            
            .Dialogue("scene_assessment", "Scene Assessment", 
                "Your {specialty} background will be valuable here. You arrive to find 15-20 injured people scattered around the collapsed stage. " +
                "Some are crying, others are unconscious. What's your first priority?")
            .Choice("Establish incident command and triage", "triage", "triage_setup", "first_action")
            .Choice("Treat the most severely injured first", "severe", "severe_treatment", "first_action")
            .Choice("Ensure scene safety and request resources", "safety", "safety_first", "first_action")
            
            .Dialogue("triage_setup", "Triage Protocol", 
                "Good! Your {specialty} training guides your triage decisions. You've identified several patient categories. " +
                "A young woman has a suspected spinal injury but is conscious. How do you classify her?")
            .DropdownChoice("Priority 1 (Red) - Immediate", "red")
            .DropdownChoice("Priority 2 (Yellow) - Delayed", "yellow", "triage_decision")
            .DropdownChoice("Priority 3 (Green) - Minor", "green")
            .ConditionalNext(state => "treatment_phase")
            
            .Dialogue("severe_treatment", "Direct Treatment", 
                "You rush to help a man with obvious chest trauma. Your {specialty} expertise tells you he needs immediate attention. " +
                "His breathing is labored. What's your treatment priority?")
            .DropdownChoice("Establish airway control", "airway")
            .DropdownChoice("Control bleeding", "bleeding", "triage_decision")
            .DropdownChoice("IV access and fluids", "iv")
            .ConditionalNext(state => "treatment_phase")
            
            .Dialogue("safety_first", "Scene Safety", 
                "Excellent! Your {specialty} background emphasizes safety first. You notice electrical wires near the stage " +
                "and unstable debris. After securing the scene, how do you organize patient care?")
            .DropdownChoice("Set up treatment areas by injury type", "organized")
            .DropdownChoice("Treat patients where they are", "in_place", "triage_decision")
            .DropdownChoice("Move all patients to a central area", "central")
            .ConditionalNext(state => "treatment_phase")
            
            .Dialogue("treatment_phase", "Medical Response Complete", 
                "Outstanding work, Dr. {playerName}! Your {first_action} approach and {triage_decision} protocol, " +
                "combined with your {specialty} expertise, helped save multiple lives today. All critical patients " +
                "were stabilized and transported successfully. The incident command praised your quick thinking and " +
                "systematic approach. You've made a real difference!")
            
            .Build();
    }
    
    /// <summary>
    /// Gets all available example stories
    /// </summary>
    public static List<Story> GetAllStories()
    {
        return new List<Story>
        {
            CreateFirefighterStory(),
            CreateMedicalStory()
        };
    }
    
    /// <summary>
    /// Registers all example stories with the story engine
    /// </summary>
    public static void RegisterAllStories(StoryEngine engine)
    {
        engine.RegisterStory(CreateFirefighterStory());
        engine.RegisterStory(CreateMedicalStory());
    }
}