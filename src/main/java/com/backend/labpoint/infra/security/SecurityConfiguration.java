package com.backend.labpoint.infra.security;

import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.http.HttpMethod;
import org.springframework.security.authentication.AuthenticationManager;
import org.springframework.security.config.annotation.authentication.configuration.AuthenticationConfiguration;
import org.springframework.security.config.annotation.web.builders.HttpSecurity;
import org.springframework.security.config.annotation.web.configuration.EnableWebSecurity;
import org.springframework.security.config.http.SessionCreationPolicy;
import org.springframework.security.crypto.bcrypt.BCryptPasswordEncoder;
import org.springframework.security.crypto.password.PasswordEncoder;
import org.springframework.security.config.Customizer;
import org.springframework.security.web.SecurityFilterChain;
import org.springframework.security.web.authentication.UsernamePasswordAuthenticationFilter;

@Configuration
@EnableWebSecurity
public class SecurityConfiguration {
    @Autowired
    private SecurityFilter securityFilter;

    @Bean
    public SecurityFilterChain securityFilterChain(HttpSecurity http) throws Exception {
        return http.
                csrf(csrf -> csrf.disable())
                .cors(Customizer.withDefaults()) // Enable CORS support in Spring Security
                .sessionManagement(sessionManagement -> sessionManagement.sessionCreationPolicy(SessionCreationPolicy.STATELESS))
                .authorizeHttpRequests(authorizeRequests ->
                        authorizeRequests
                                // Documentation
                                .requestMatchers(HttpMethod.GET, "/docs/**").permitAll()
                                .requestMatchers(HttpMethod.GET, "/v3/**").permitAll()
                                .requestMatchers(HttpMethod.GET, "/swagger-ui/**").permitAll()
                                // Authentication
                                .requestMatchers(HttpMethod.GET, "/auth/users").hasRole("ADMIN")
                                .requestMatchers(HttpMethod.POST, "/auth/login").permitAll()
                                .requestMatchers(HttpMethod.POST, "/auth/register").permitAll()
                                .requestMatchers(HttpMethod.GET, "/auth/update").hasRole("ADMIN")
                                // Spaces
                                .requestMatchers(HttpMethod.POST, "/spaces/create").hasRole("ADMIN")
                                .requestMatchers(HttpMethod.PATCH, "/spaces/update/**").hasRole("ADMIN")
                                .requestMatchers(HttpMethod.DELETE, "/spaces/delete/**").hasRole("ADMIN")
                                // Resources
                                .requestMatchers(HttpMethod.POST, "/resources/create").hasRole("ADMIN")
                                .requestMatchers(HttpMethod.PATCH, "/resources/update/**").hasRole("ADMIN")
                                .requestMatchers(HttpMethod.DELETE, "/resources/delete/**").hasRole("ADMIN")
                                // Subject
                                .requestMatchers(HttpMethod.POST, "/subjects/create").hasRole("ADMIN")
                                .requestMatchers(HttpMethod.PATCH, "/subjects/update/**").hasRole("ADMIN")
                                .requestMatchers(HttpMethod.DELETE, "/subjects/delete/**").hasRole("ADMIN")
                                .anyRequest().authenticated()
                )
                .addFilterBefore(securityFilter, UsernamePasswordAuthenticationFilter.class)
                .build();
    }

    @Bean
    public AuthenticationManager authenticationManager(AuthenticationConfiguration authenticationConfiguration) throws Exception {
        return authenticationConfiguration.getAuthenticationManager();
    }

    @Bean
    public PasswordEncoder passwordEncoder() {
        return new BCryptPasswordEncoder();
    }
}
