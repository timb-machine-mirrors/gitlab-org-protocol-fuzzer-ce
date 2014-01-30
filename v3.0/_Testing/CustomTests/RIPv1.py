if any(opsys in get_platform() for opsys in ['osx', 'win']):
    skip_tests.append('RIPv1')
