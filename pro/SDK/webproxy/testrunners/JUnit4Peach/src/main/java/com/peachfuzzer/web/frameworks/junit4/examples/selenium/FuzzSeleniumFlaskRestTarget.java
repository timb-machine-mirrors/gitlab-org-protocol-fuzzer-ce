package com.peachfuzzer.web.frameworks.junit4.examples.selenium;

import com.peachfuzzer.web.frameworks.junit4.JUnit4PeachSuiteRunner.PeachSuiteClasses;
import org.junit.BeforeClass;
import org.junit.runner.RunWith;

@RunWith(com.peachfuzzer.web.frameworks.junit4.JUnit4PeachSuiteRunner.class)
@PeachSuiteClasses({
    RestTargetGetallusers.class,
    RestTargetCreateuser.class,
    RestTargetUpdateuser.class})
public class FuzzSeleniumFlaskRestTarget {
    @BeforeClass
    public static void setProxy()
    {
        System.setProperty("http_proxy", "http://127.0.0.1:8001");
        System.setProperty("ssl_proxy", "http://127.0.0.1:8001");
        
        System.setProperty("webdriver.gecko.driver", "c:\\temp\\geckodriver.exe");
    }
    
}
