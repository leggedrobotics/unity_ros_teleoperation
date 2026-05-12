// In a file like ScreenScale.m
#import <UIKit/UIKit.h>
#import <objc/runtime.h>

#include "DisplayManager.h"

@interface DisplayConnection (InjectOverscanCompensation)
@end

@implementation DisplayConnection (InjectOverscanCompensation)

+ (void)load {
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{
        Class class = [self class];

        SEL originalSelector = @selector(initRendering);
        SEL swizzledSelector = @selector(xxx_initRendering);

        Method originalMethod = class_getInstanceMethod(class, originalSelector);
        Method swizzledMethod = class_getInstanceMethod(class, swizzledSelector);

        BOOL didAddMethod = class_addMethod(class,
                                            originalSelector,
                                            method_getImplementation(swizzledMethod),
                                            method_getTypeEncoding(swizzledMethod));

        if (didAddMethod) {
            class_replaceMethod(class,
                                swizzledSelector,
                                method_getImplementation(originalMethod),
                                method_getTypeEncoding(originalMethod));
        } else {
            method_exchangeImplementations(originalMethod, swizzledMethod);
        }
    });
}

- (UnityDisplaySurfaceBase*)xxx_initRendering {
    // Turn off overscan compensation
    self.screen.overscanCompensation = UIScreenOverscanCompensationNone;

    // Call the original initRendering method
    UnityDisplaySurfaceBase *result = [self xxx_initRendering];

    return result;
}

@end
