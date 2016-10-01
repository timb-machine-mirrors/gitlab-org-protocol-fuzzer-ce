package com.peachfuzzer.web.frameworks.junit4.examples.selenium;

import org.openqa.selenium.WebDriver;
import org.openqa.selenium.firefox.FirefoxDriver;
import org.openqa.selenium.remote.CapabilityType;
import org.openqa.selenium.remote.DesiredCapabilities;
import org.openqa.selenium.Proxy.ProxyType;
import org.openqa.selenium.firefox.FirefoxProfile;


public class SeleniumTestBase {
    /*
    public WebDriver getFirefoxDriver()
    {
        String http_proxy = System.getProperty("http_proxy");
        String https_proxy = System.getProperty("http_proxy");
        
        if(http_proxy != null || https_proxy != null)
        {
            FirefoxProfile profile = new FirefoxProfile();
            profile.setPreference("network.proxy.type", 1);
            profile.setPreference("network.proxy.http", "127.0.0.1");
            profile.setPreference("network.proxy.http_port", 8001);
            profile.setPreference("network.proxy.ssl", "127.0.0.1");
            profile.setPreference("network.proxy.ssl_port", 80001);
            
            return new FirefoxDriver(profile);
        }

         return new FirefoxDriver();
    }*/
    
    public WebDriver getFirefoxDriver()
    {
        DesiredCapabilities cap = new DesiredCapabilities();
        
        String http_proxy = System.getProperty("http_proxy");
        String https_proxy = System.getProperty("http_proxy");
        
        if(http_proxy != null || https_proxy != null)
        {
            org.openqa.selenium.Proxy proxy = new org.openqa.selenium.Proxy();
            
            if(http_proxy != null)
                proxy.setHttpProxy(http_proxy);
            if(https_proxy != null)
                proxy.setSslProxy(https_proxy);
            
            cap.setCapability(CapabilityType.PROXY, proxy);
        }

         return new FirefoxDriver(cap);
    }
}
