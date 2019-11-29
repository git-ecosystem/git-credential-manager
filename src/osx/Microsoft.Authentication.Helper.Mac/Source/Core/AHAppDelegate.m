// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#import "AHAppDelegate.h"
#import <Cocoa/Cocoa.h>

extern const NSString* kErrorDomain;

@implementation AHAppDelegate

-(id)initWithBlock:(AHAppWorkBlock)block logger:(AHLogger*)logger;
{
    self = [super init];
    if (self != nil)
    {
        _workDelegateBlock = block;
        _logger = logger;
    }

    return self;
}

-(void)run
{
    NSApplication *application = [NSApplication sharedApplication];
    NSMenu *mainMenu = [self createMainMenu];
    [application setMainMenu:mainMenu];

    [self setApplication:application];
    [application setDelegate:self];
    [NSApp setActivationPolicy:NSApplicationActivationPolicyRegular];

    AHLOG(_logger, @"Creating application context for authentication prompt");

    [NSApp run];
}

-(void)applicationWillFinishLaunching:(NSNotification *)notification
{
    [[self application] activateIgnoringOtherApps:YES];
    self->_workDelegateBlock();
}

-(void) stop
{
    [[self application] stop:self];

    // Send an event to the app, because "stop" only stops running the
    // delegate after processing an event.
    NSPoint p = CGPointZero;
    NSEvent* event = [NSEvent otherEventWithType:NSEventTypeApplicationDefined
                                        location:p
                                   modifierFlags:0
                                       timestamp:0
                                    windowNumber:0
                                         context:0
                                         subtype:0
                                           data1:0
                                           data2:0];

    [[self application] postEvent:event atStart:NO];
}

-(NSMenu*) createMainMenu
{
    NSMenu *mainMenu = [NSMenu new];

    // Create top-level menu items
    NSMenuItem *appMenuItem = [NSMenuItem new];
    NSMenuItem *editMenuItem = [NSMenuItem new];
    [mainMenu addItem:appMenuItem];
    [mainMenu addItem:editMenuItem];

    // Create app menu items
    NSMenu *appMenu = [NSMenu new];
    [appMenuItem setSubmenu:appMenu];
    [appMenu addItemWithTitle:@"Quit"
                       action:@selector(terminate:)
                keyEquivalent:@"q"];

    // Create edit menu items
    NSMenu *editMenu = [[NSMenu alloc] initWithTitle:@"Edit"];
    [editMenuItem setSubmenu:editMenu];
    [editMenu addItemWithTitle:@"Cut" action:@selector(cut:) keyEquivalent:@"x"];
    [editMenu addItemWithTitle:@"Copy" action:@selector(copy:) keyEquivalent:@"c"];
    [editMenu addItemWithTitle:@"Paste" action:@selector(paste:) keyEquivalent:@"v"];
    [editMenu addItemWithTitle:@"Delete" action:@selector(delete:) keyEquivalent:@""];
    [editMenu addItemWithTitle:@"Select All" action:@selector(selectAll:) keyEquivalent:@"a"];

    return mainMenu;
}

+ (NSError*)runDelegate:(AHAppWorkBlock)completionBlock logger:(AHLogger*)logger
{
    AHAppDelegate * delegate = [[AHAppDelegate alloc] initWithBlock:completionBlock logger:logger];

    [delegate run];

    return [delegate error];
}

@end
