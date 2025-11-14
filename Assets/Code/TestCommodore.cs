using Commodore;
using System;
using System.Collections.Generic;
using static NumberGuessingGame;
using UnityEngine;

public class TestCommodore : CommodoreBehavior
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    protected override string ProcessCommand(string command)
    {
        if (command == "HELLO")
            return "Hello, World!";
        return $"I DO NOT UNDERSTAND \"{command}\"";
    }
}
