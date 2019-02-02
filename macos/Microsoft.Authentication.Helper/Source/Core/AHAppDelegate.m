// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#import "AHAppDelegate.h"

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
    NSApplication * application = [NSApplication sharedApplication];
    [self setApplication:application];
    [application setDelegate:self];
    [NSApp setActivationPolicy:NSApplicationActivationPolicyRegular];

    if ([self userLoggingAllowed])
    {
        [_logger log:@"Creating application context for authentication prompt"];
    }

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

+ (NSError*)runDelegate:(AHAppWorkBlock)completionBlock logger:(AHLogger*)logger
{
    AHAppDelegate * delegate = [[AHAppDelegate alloc] initWithBlock:completionBlock logger:logger];

    [delegate run];

    return [delegate error];
}

@end
