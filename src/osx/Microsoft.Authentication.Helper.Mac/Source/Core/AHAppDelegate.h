// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#import <AppKit/AppKit.h>
#import "AHLogger.h"

typedef void(^AHAppWorkBlock) (void);

@interface AHAppDelegate :NSObject <NSApplicationDelegate>
{
    AHAppWorkBlock _workDelegateBlock;
    AHLogger* _logger;
}

@property (retain, nonatomic) NSApplication* application;
@property (retain, nonatomic) NSError* error;

-(id)initWithBlock:(AHAppWorkBlock)block logger:(AHLogger*)logger;

-(void)run;
-(void)stop;

+ (NSError*)runDelegate:(AHAppWorkBlock)completionBlock logger:(AHLogger*)logger;

@end
