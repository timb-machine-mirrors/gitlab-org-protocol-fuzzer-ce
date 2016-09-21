/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package com.peachfuzzer.web.frameworks.junit4;

import com.peachfuzzer.api.Proxy;
import org.junit.runners.model.FrameworkMethod;
import org.junit.runners.model.Statement;

public class PeachInvokeMethod extends Statement {
    private final FrameworkMethod testMethod;
    private final Object target;
    private final Proxy proxy;
    private final String name;

    public PeachInvokeMethod(Proxy proxy, String name, FrameworkMethod testMethod, Object target) {
        this.testMethod = testMethod;
        this.target = target;
        this.proxy = proxy;
        this.name = name;
    }

    @Override
    public void evaluate() throws Throwable {
        
        proxy.testCase(name);
        testMethod.invokeExplosively(target);
    }
}