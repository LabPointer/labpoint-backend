package com.backend.labpoint.infra.security;

import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.http.HttpMethod;
import org.springframework.security.authentication.AuthenticationManager;
import org.springframework.security.config.Customizer;
import org.springframework.security.config.annotation.authentication.configuration.AuthenticationConfiguration;
import org.springframework.security.config.annotation.web.builders.HttpSecurity;
import org.springframework.security.config.annotation.web.configuration.EnableWebSecurity;
import org.springframework.security.config.http.SessionCreationPolicy;
import org.springframework.security.crypto.bcrypt.BCryptPasswordEncoder;
import org.springframework.security.crypto.password.PasswordEncoder;
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
                                .requestMatchers(HttpMethod.GET, "/v1/**").permitAll()
                                // Authentication
                                .requestMatchers(HttpMethod.GET, "/auth/users").hasRole("ADMIN")
                                .requestMatchers(HttpMethod.POST, "/auth/sign-in").permitAll()
                                .requestMatchers(HttpMethod.POST, "/auth/sign-up").permitAll()
                                .requestMatchers(HttpMethod.POST, "/auth/sign-out").permitAll()
                                .requestMatchers(HttpMethod.POST, "/auth/forgot-password").permitAll()
                                .requestMatchers(HttpMethod.PATCH, "/auth/reset-password/**").permitAll()
                                .requestMatchers(HttpMethod.GET, "/auth/update").hasRole("ADMIN")
                                // Spaces
                                .requestMatchers(HttpMethod.POST, "/manage/spaces/create").hasRole("ADMIN")
                                .requestMatchers(HttpMethod.PATCH, "/manage/spaces/update/**").hasRole("ADMIN")
                                .requestMatchers(HttpMethod.DELETE, "/manage/spaces/delete/**").hasRole("ADMIN")
                                // Resources
                                .requestMatchers(HttpMethod.POST, "/manage/resources/create").hasRole("ADMIN")
                                .requestMatchers(HttpMethod.PATCH, "/manage/resources/update/**").hasRole("ADMIN")
                                .requestMatchers(HttpMethod.DELETE, "/manage/resources/delete/**").hasRole("ADMIN")
                                // Subjects
                                .requestMatchers(HttpMethod.POST, "/manage/subjects/create").hasRole("ADMIN")
                                .requestMatchers(HttpMethod.PATCH, "/manage/subjects/update/**").hasRole("ADMIN")
                                .requestMatchers(HttpMethod.DELETE, "/manage/subjects/delete/**").hasRole("ADMIN")
                                // Accounts
                                .requestMatchers(HttpMethod.GET, "/account/manage/search").hasRole("ADMIN")
                                .requestMatchers(HttpMethod.POST, "/account/manage/update").hasRole("ADMIN")
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
