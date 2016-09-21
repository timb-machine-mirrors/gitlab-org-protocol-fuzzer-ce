/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package com.peachfuzzer.web.frameworks.junit4;

import com.peachfuzzer.api.Proxy;
import java.util.List;

import org.junit.runners.model.FrameworkMethod;
import org.junit.runners.model.Statement;

public class PeachRunBefores extends Statement {
    private final Statement next;
    private final Object target;
    private final List<FrameworkMethod> befores;
    private final Proxy proxy;

    public PeachRunBefores(Proxy proxy, Statement next, List<FrameworkMethod> befores, Object target) {
        this.next = next;
        this.befores = befores;
        this.target = target;
        this.proxy = proxy;
    }

    @Override
    public void evaluate() throws Throwable {
        
        proxy.testSetUp();
        
        for (FrameworkMethod before : befores) {
            before.invokeExplosively(target);
        }
        next.evaluate();
    }
}